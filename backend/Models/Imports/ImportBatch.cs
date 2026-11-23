namespace Fourway.Api.Models;

/// <summary>
/// Jedna importovaná datová dávka JDF.
/// </summary>
/// <remarks>
/// Slouží k dohledatelnosti původu dat a k mapování interních čísel zastávek
/// z konkrétní dávky na kanonické <see cref="Stop"/>.
/// </remarks>
public sealed class ImportBatch
{
    /// <summary>Maximální délka cesty ke zdrojovému souboru.</summary>
    public const int SourcePathMaxLength = 500;

    /// <summary>Maximální délka hashe zdrojového souboru.</summary>
    public const int FileHashMaxLength = 128;

    /// <summary>Maximální délka deklarované verze JDF.</summary>
    public const int JdfVersionMaxLength = 10;

    /// <summary>Identifikátor dávky.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Cesta ke zdrojovému JDF zipu v době importu.
    /// </summary>
    public string SourcePath { get; private set; } = null!;

    /// <summary>
    /// Hash zdrojového souboru pro rozpoznání opakovaného importu.
    /// </summary>
    public string FileHash { get; private set; } = null!;

    /// <summary>
    /// Verze formátu JDF deklarovaná v <c>VerzeJDF.txt</c>, například <c>1.11</c>.
    /// </summary>
    public string JdfVersion { get; private set; } = null!;

    /// <summary>
    /// Datum exportu dávky z <c>VerzeJDF.txt</c>, pokud je uvedeno.
    /// </summary>
    public DateOnly? ExportedOn { get; private set; }

    /// <summary>
    /// Okamžik, kdy byla dávka uložena do databáze.
    /// </summary>
    public DateTimeOffset ImportedAt { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private ImportBatch()
    {
    }

    /// <summary>
    /// Vytvoří novou importní dávku.
    /// </summary>
    /// <param name="sourcePath">Cesta ke zdrojovému souboru.</param>
    /// <param name="fileHash">Hash zdrojového souboru.</param>
    /// <param name="jdfVersion">Verze formátu JDF.</param>
    /// <param name="exportedOn">Datum exportu dávky, pokud je známé.</param>
    public ImportBatch(
        string sourcePath,
        string fileHash,
        string jdfVersion,
        DateOnly? exportedOn = null)
    {
        Id = Guid.CreateVersion7();
        SourcePath = DomainValidation.Required(sourcePath, nameof(sourcePath), SourcePathMaxLength);
        FileHash = DomainValidation.Required(fileHash, nameof(fileHash), FileHashMaxLength);
        JdfVersion = DomainValidation.Required(jdfVersion, nameof(jdfVersion), JdfVersionMaxLength);
        ExportedOn = exportedOn;
        ImportedAt = DateTimeOffset.UtcNow;
    }
}
