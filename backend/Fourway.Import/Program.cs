using System.Diagnostics;
using Fourway.Import.Configuration;
using Fourway.Import.Extraction;
using Fourway.Import.Services;
using Fourway.Import.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = LoadAppSettings();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        options.UseUtcTimestamp = false;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("Fourway.Import");

var options = new ImportOptions();
string? connectionString = null;

for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    if (arg.Equals("--file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        options.LocalZipPath = args[++i];
    }
    else if (arg.Equals("--url", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        options.JdfUrl = args[++i];
    }
    else if (arg.Equals("--connection-string", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        connectionString = args[++i];
    }
    else if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
    {
        options.DryRun = true;
    }
    else if (arg.Equals("--reset", StringComparison.OrdinalIgnoreCase))
    {
        options.ResetDatabase = true;
    }
    else if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
    {
        PrintUsage();
        return 0;
    }
}

logger.LogInformation("=== Fourway.Import CLI ===");

connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION")
    ?? configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString) && !options.DryRun)
{
    logger.LogError(
        "Nebyl zadán žádný databázový connection string. " +
        "Nastavte proměnnou prostředí ConnectionStrings__DefaultConnection nebo předejte parametr --connection-string. " +
        "Pro testování bez DB můžete použít přepínač --dry-run.");
    return 1;
}

string? downloadedTempFile = null;
var archivePath = options.LocalZipPath;

try
{
    if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
    {
        var tempDir = options.TempDirectory ?? Path.GetTempPath();
        downloadedTempFile = Path.Combine(tempDir, $"jdf_{Guid.NewGuid():N}.zip");

        var downloader = new JdfArchiveDownloader(loggerFactory.CreateLogger<JdfArchiveDownloader>());
        var downloadSw = Stopwatch.StartNew();
        archivePath = await downloader.DownloadAsync(options.JdfUrl, downloadedTempFile);
        downloadSw.Stop();
        logger.LogInformation("Stahování trvalo {ElapsedSeconds:F1} s.", downloadSw.Elapsed.TotalSeconds);
    }
    else
    {
        logger.LogInformation("Používám lokální archiv: {ArchivePath}", archivePath);
    }

    if (!File.Exists(archivePath))
    {
        logger.LogError("Archiv {ArchivePath} neexistuje.", archivePath);
        return 1;
    }

    await using var bulkImporter = new PostgresBulkImporter(
        connectionString ?? "Host=localhost;Database=dummy",
        loggerFactory.CreateLogger<PostgresBulkImporter>());
    var orchestrator = new JdfImportOrchestrator(
        bulkImporter,
        options,
        loggerFactory.CreateLogger<JdfImportOrchestrator>());

    await orchestrator.RunAsync(archivePath);

    return 0;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Import selhal: {ErrorMessage}", ex.Message);
    return 1;
}
finally
{
    if (downloadedTempFile is not null && File.Exists(downloadedTempFile))
    {
        try
        {
            File.Delete(downloadedTempFile);
            logger.LogInformation("Dočasný archiv byl smazán.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Dočasný archiv {ArchivePath} se nepodařilo smazat.", downloadedTempFile);
        }
    }
}

static IConfiguration LoadAppSettings()
{
    var builder = new ConfigurationBuilder();
    foreach (var directory in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        builder.AddJsonFile(Path.Combine(directory, "appsettings.json"), optional: true);
    }

    // Allows overriding any setting (including Logging:LogLevel) from the environment,
    // e.g. Logging__LogLevel__Fourway.Import=Debug.
    builder.AddEnvironmentVariables();

    return builder.Build();
}

static void PrintUsage()
{
    Console.WriteLine("""
        Použití: Fourway.Import [přepínače]

        Přepínače:
          --file <cesta>               Cesta k lokálnímu JDF.zip (přeskočí stahování ze sítě)
          --url <adresa>               Vlastní URL pro stažení JDF.zip
          --connection-string <conn>   Connection string k PostgreSQL
          --dry-run                    Pouze parsování a hashování, nezapisuje do databáze
          --reset                      Před importem vyprázdní databázi (TRUNCATE všech tabulek)
          --help, -h                   Zobrazí tuto nápovědu

        Proměnné prostředí:
          ConnectionStrings__DefaultConnection   Výchozí connection string
          Logging__LogLevel__Fourway.Import      Úroveň logování (např. Debug)
        """);
}
