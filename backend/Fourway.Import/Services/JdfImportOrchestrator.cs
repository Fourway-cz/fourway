using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Fourway.Import.Configuration;
using Fourway.Import.Parsing;
using Fourway.Import.Storage;

namespace Fourway.Import.Services;

public sealed class JdfImportOrchestrator
{
    private readonly PostgresBulkImporter _bulkImporter;
    private readonly ImportOptions _options;

    public JdfImportOrchestrator(PostgresBulkImporter bulkImporter, ImportOptions options)
    {
        _bulkImporter = bulkImporter;
        _options = options;
    }

    public async Task RunAsync(string outerZipPath, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"Inicializuji import z archivu: {outerZipPath}");

        if (!_options.DryRun)
        {
            if (_options.ResetDatabase)
            {
                Console.WriteLine("Mažu databázi (--reset)...");
                await _bulkImporter.ResetDatabaseAsync(ct);
                Console.WriteLine("Databáze vyprázdněna.");
            }

            await _bulkImporter.InitializeAsync(ct);
        }
        else
        {
            Console.WriteLine("[DRY-RUN] Režim simulace aktivní (databáze se neotevírá).");
        }

        using var outerZip = ZipFile.OpenRead(outerZipPath);
        var totalEntries = outerZip.Entries.Count(e => e.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"Archiv obsahuje {totalEntries} vnitřních linek/ZIP souborů.");

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

            try
            {
                var fileHash = await ComputeEntryHashAsync(entry, ct);
                if (_bulkImporter.IsHashAlreadyImported(fileHash))
                {
                    skipped++;
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
                }
                else
                {
                    var routeName = package.Linky.FirstOrDefault()?.NazevLinky ?? "Neznámá linka";
                    Console.WriteLine($"[DRY-RUN] Úspěšně zpracován balíček: {package.ArchiveName} -> Linka: '{routeName}' (Zastávky: {package.Zastavky.Count}, Trasa: {package.Zaslinky.Count}, Spoje: {package.Spoje.Count}, Časy: {package.Zasspoje.Count}, IDS: {package.LinExt.Count})");
                }

                imported++;
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine($"Chyba při zpracování {entry.Name}: {ex.Message}");
            }

            ReportProgress(processed, totalEntries, imported, skipped, errors, sw);
        }

        sw.Stop();
        var elapsed = sw.Elapsed;
        var rate = processed / Math.Max(0.1, elapsed.TotalSeconds);
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"Import dokončen za {FormatElapsed(elapsed)}.");
        Console.WriteLine($"Rychlost:        {rate:F1} balíčků/s");
        Console.WriteLine($"Celkem balíčků: {totalEntries}");
        Console.WriteLine($"Importováno:     {imported}");
        Console.WriteLine($"Přeskočeno:     {skipped}");
        Console.WriteLine($"Chyb:           {errors}");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine($"Zastávky:          {totalStops:N0}");
        Console.WriteLine($"Zastávky v trase:  {totalRouteStops:N0}");
        Console.WriteLine($"Spoje:             {totalTrips:N0}");
        Console.WriteLine($"Časy zastavení:   {totalStopTimes:N0}");
        Console.WriteLine("========================================");
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

    private static void ReportProgress(
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
            Console.WriteLine(
                $"[{processed}/{total}] ({(processed * 100) / total} %) " +
                $"Importováno: {imported}, Přeskočeno: {skipped}, Chyb: {errors} " +
                $"({rate:F0} balíčků/s)");
        }
    }
}
