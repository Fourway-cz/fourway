using Fourway.Import.Configuration;
using Fourway.Import.Extraction;
using Fourway.Import.Services;
using Fourway.Import.Storage;

Console.WriteLine("=== Fourway.Import CLI ===");

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
    else if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
    {
        PrintUsage();
        return 0;
    }
}

connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString) && !options.DryRun)
{
    Console.Error.WriteLine(
        "Chyba: Nebyl zadán žádný databázový connection string.\n" +
        "Nastavte proměnnou prostředí ConnectionStrings__DefaultConnection nebo předejte parametr --connection-string.\n" +
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

        var downloader = new JdfArchiveDownloader();
        archivePath = await downloader.DownloadAsync(options.JdfUrl, downloadedTempFile);
    }
    else
    {
        Console.WriteLine($"Používám lokální archiv: {archivePath}");
    }

    if (!File.Exists(archivePath))
    {
        Console.Error.WriteLine($"Chyba: Archiv '{archivePath}' neexistuje.");
        return 1;
    }

    await using var bulkImporter = new PostgresBulkImporter(connectionString ?? "Host=localhost;Database=dummy");
    var orchestrator = new JdfImportOrchestrator(bulkImporter, options);

    await orchestrator.RunAsync(archivePath);

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"\n[FATAL ERROR] {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
finally
{
    if (downloadedTempFile is not null && File.Exists(downloadedTempFile))
    {
        try
        {
            File.Delete(downloadedTempFile);
            Console.WriteLine("Dočasný archiv byl smazán.");
        }
        catch
        {
            // Ignore cleanup failure
        }
    }
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
          --help, -h                   Zobrazí tuto nápovědu

        Proměnné prostředí:
          ConnectionStrings__DefaultConnection   Výchozí connection string
        """);
}
