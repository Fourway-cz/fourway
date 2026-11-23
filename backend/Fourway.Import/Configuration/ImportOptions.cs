namespace Fourway.Import.Configuration;

public sealed class ImportOptions
{
    public const string DefaultJdfUrl = "https://portal.cisjr.cz/pub/JDF/JDF.zip";

    public string JdfUrl { get; set; } = DefaultJdfUrl;

    public string? LocalZipPath { get; set; }

    public string? TempDirectory { get; set; }

    public bool DryRun { get; set; }

    public int BatchCommitSize { get; set; } = 100;
}
