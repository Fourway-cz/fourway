using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Fourway.Import.Configuration;
using Fourway.Import.Parsing;
using Fourway.Import.Storage;
using Microsoft.Extensions.Logging;

namespace Fourway.Import.Services;

public sealed class JdfImportOrchestrator
{
    private readonly PostgresBulkImporter _bulkImporter;
    private readonly ImportOptions _options;
    private readonly ILogger<JdfImportOrchestrator> _logger;

    public JdfImportOrchestrator(
        PostgresBulkImporter bulkImporter,
        ImportOptions options,
        ILogger<JdfImportOrchestrator> logger)
    {
        _bulkImporter = bulkImporter;
        _options = options;
        _logger = logger;
    }

    public async Task RunAsync(string outerZipPath, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Inicializuji import z archivu: {ArchivePath}", outerZipPath);

        if (!_options.DryRun)
        {
            if (_options.ResetDatabase)
            {
                _logger.LogInformation("Mažu databázi (--reset)...");
                await _bulkImporter.ResetDatabaseAsync(ct);
                _logger.LogInformation("Databáze vyprázdněna.");
            }

            await _bulkImporter.InitializeAsync(ct);
        }
        else
        {
            _logger.LogInformation("[DRY-RUN] Režim simulace aktivní (databáze se neotevírá).");
        }

        using var outerZip = ZipFile.OpenRead(outerZipPath);
        var totalEntries = outerZip.Entries.Count(e => e.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        _logger.LogInformation("Archiv obsahuje {TotalEntries} vnitřních linek/ZIP souborů.", totalEntries);

        var processed = 0;
        var skipped = 0;
        var imported = 0;
        var errors = 0;
        long totalStops = 0;
        long totalRouteStops = 0;
        long totalTrips = 0;
        long totalStopTimes = 0;

        foreach (var entry in outerZip.Entries)
        {
            if (!entry.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ct.ThrowIfCancellationRequested();
            processed++;
            string? fileHash = null;

            try
            {
                fileHash = await ComputeEntryHashAsync(entry, ct);
                if (_bulkImporter.IsHashAlreadyImported(fileHash))
                {
                    skipped++;
                    _logger.LogDebug(
                        "Přeskakuji {EntryName}: balíček s hashem {FileHash} už byl dříve importován.",
                        entry.Name,
                        fileHash);
                    ReportProgress(processed, totalEntries, imported, skipped, errors, sw);
                    continue;
                }

                using var innerZipStream = new MemoryStream();
                await using (var entryStream = entry.Open())
                {
                    await entryStream.CopyToAsync(innerZipStream, ct);
                }
                innerZipStream.Position = 0;

                using var innerArchive = new ZipArchive(innerZipStream, ZipArchiveMode.Read, leaveOpen: false);
                var package = JdfPackageParser.Parse(innerArchive, entry.Name, fileHash);

                totalStops += package.Zastavky.Count;
                totalRouteStops += package.Zaslinky.Count;
                totalTrips += package.Spoje.Count;
                totalStopTimes += package.Zasspoje.Count;

                if (!_options.DryRun)
                {
                    await _bulkImporter.ImportPackageAsync(package, ct);
                    _logger.LogDebug(
                        "Balíček {EntryName} importován (zastávky: {StopCount}, trasa: {RouteStopCount}, spoje: {TripCount}, časy: {StopTimeCount}, IDS: {IdsCount}).",
                        entry.Name,
                        package.Zastavky.Count,
                        package.Zaslinky.Count,
                        package.Spoje.Count,
                        package.Zasspoje.Count,
                        package.LinExt.Count);
                }
                else
                {
                    var routeName = package.Linky.FirstOrDefault()?.NazevLinky ?? "Neznámá linka";
                    _logger.LogInformation(
                        "[DRY-RUN] Úspěšně zpracován balíček: {ArchiveName} -> Linka: '{RouteName}' (Zastávky: {StopCount}, Trasa: {RouteStopCount}, Spoje: {TripCount}, Časy: {StopTimeCount}, IDS: {IdsCount})",
                        package.ArchiveName,
                        routeName,
                        package.Zastavky.Count,
                        package.Zaslinky.Count,
                        package.Spoje.Count,
                        package.Zasspoje.Count,
                        package.LinExt.Count);
                }

                imported++;
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(
                    ex,
                    "Chyba při zpracování balíčku {EntryName} (hash: {FileHash}).",
                    entry.Name,
                    fileHash ?? "neznámý");
            }

            ReportProgress(processed, totalEntries, imported, skipped, errors, sw);
        }

        sw.Stop();
        var elapsed = sw.Elapsed;
        var rate = processed / Math.Max(0.1, elapsed.TotalSeconds);
        _logger.LogInformation(
            "Import dokončen za {Elapsed}. Rychlost: {Rate:F1} balíčků/s. " +
            "Celkem balíčků: {TotalEntries}, importováno: {Imported}, přeskočeno: {Skipped} (hash už je v import_batches), chyb: {Errors}.",
            FormatElapsed(elapsed),
            rate,
            totalEntries,
            imported,
            skipped,
            errors);
        _logger.LogInformation(
            "Zastávky: {Stops:N0}, zastávky v trase: {RouteStops:N0}, spoje: {Trips:N0}, časy zastavení: {StopTimes:N0}.",
            totalStops,
            totalRouteStops,
            totalTrips,
            totalStopTimes);
    }

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours} h {elapsed.Minutes} min {elapsed.Seconds} s"
            : $"{elapsed.Minutes} min {elapsed.Seconds} s";

    private static async Task<string> ComputeEntryHashAsync(ZipArchiveEntry entry, CancellationToken ct)
    {
        await using var entryStream = entry.Open();
        var hashBytes = await SHA256.HashDataAsync(entryStream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private void ReportProgress(
        int processed,
        int total,
        int imported,
        int skipped,
        int errors,
        Stopwatch sw)
    {
        if (processed % 250 == 0 || processed == total)
        {
            var rate = processed / Math.Max(0.1, sw.Elapsed.TotalSeconds);
            _logger.LogInformation(
                "[{Processed}/{Total}] ({Percentage} %) Importováno: {Imported}, Přeskočeno: {Skipped}, Chyb: {Errors} ({Rate:F0} balíčků/s)",
                processed,
                total,
                (processed * 100) / total,
                imported,
                skipped,
                errors,
                rate);
        }
    }
}
