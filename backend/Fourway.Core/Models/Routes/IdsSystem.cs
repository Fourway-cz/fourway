namespace Fourway.Core.Models;

/// <summary>
/// Systém integrované dopravy, například IREDO nebo PID.
/// </summary>
/// <remarks>
/// Kód odpovídá poli <c>KodDopravy</c> v <c>LinExt.txt</c>. Název v JDF není,
/// proto je volitelný a doplňuje se ze známého číselníku.
/// </remarks>
public sealed class IdsSystem
{
    /// <summary>Maximální délka kódu systému.</summary>
    public const int CodeMaxLength = 10;

    /// <summary>Maximální délka názvu systému.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Identifikátor systému.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Kód systému z JDF, například <c>30522</c> pro IREDO.
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Lidský název systému. Může být <see langword="null"/>, pokud mapování není známé.
    /// </summary>
    public string? Name { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private IdsSystem()
    {
    }

    /// <summary>
    /// Vytvoří nový systém integrované dopravy.
    /// </summary>
    public IdsSystem(string code, string? name = null)
    {
        Id = Guid.CreateVersion7();
        Code = DomainValidation.Required(code, nameof(code), CodeMaxLength);
        Name = DomainValidation.Optional(name, nameof(name), NameMaxLength);
    }

    /// <summary>
    /// Doplní nebo změní název systému.
    /// </summary>
    public void Rename(string? name)
    {
        Name = DomainValidation.Optional(name, nameof(name), NameMaxLength);
    }
}
