namespace Fourway.Api.Models;

/// <summary>
/// Dopravce, který zajišťuje spoje.
/// </summary>
/// <remarks>
/// Odpovídá záznamu v <c>Dopravci.txt</c>. Kontaktní údaje a adresa se ukládají
/// na úrovni provozovny v <see cref="AgencyBranch"/>.
/// </remarks>
public sealed class Agency
{
    /// <summary>Maximální délka IČO.</summary>
    public const int IcoMaxLength = 20;

    /// <summary>Maximální délka DIČ.</summary>
    public const int DicMaxLength = 20;

    /// <summary>Maximální délka obchodního jména.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Maximální délka jména fyzické osoby.</summary>
    public const int PersonNameMaxLength = 200;

    /// <summary>Identifikátor dopravce.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// IČO dopravce. Může být <see langword="null"/> u zahraničního dopravce.
    /// </summary>
    public string? Ico { get; private set; }

    /// <summary>
    /// DIČ dopravce, pokud je uvedeno.
    /// </summary>
    public string? Dic { get; private set; }

    /// <summary>
    /// Obchodní jméno dopravce.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Právní forma dopravce.
    /// </summary>
    public AgencyLegalForm LegalForm { get; private set; }

    /// <summary>
    /// Jméno a příjmení podnikatele u fyzické osoby.
    /// </summary>
    public string? PersonName { get; private set; }

    /// <summary>
    /// Označuje, zda je dopravce právnickou osobou.
    /// </summary>
    public bool IsLegalPerson => LegalForm == AgencyLegalForm.LegalPerson;

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private Agency()
    {
    }

    /// <summary>
    /// Vytvoří nového dopravce.
    /// </summary>
    /// <param name="ico">IČO, nebo <see langword="null"/> u zahraničního dopravce.</param>
    /// <param name="dic">DIČ, pokud je uvedeno.</param>
    /// <param name="name">Obchodní jméno dopravce.</param>
    /// <param name="legalForm">Právní forma dopravce.</param>
    /// <param name="personName">Jméno podnikatele u fyzické osoby.</param>
    public Agency(
        string? ico,
        string? dic,
        string name,
        AgencyLegalForm legalForm,
        string? personName = null)
    {
        Id = Guid.CreateVersion7();
        Ico = DomainValidation.Optional(ico, nameof(ico), IcoMaxLength);
        Dic = DomainValidation.Optional(dic, nameof(dic), DicMaxLength);
        Name = DomainValidation.Required(name, nameof(name), NameMaxLength);
        LegalForm = legalForm;
        PersonName = DomainValidation.Optional(personName, nameof(personName), PersonNameMaxLength);

        if (LegalForm == AgencyLegalForm.NaturalPerson && PersonName is null)
        {
            throw new ArgumentException(
                "U fyzické osoby musí být vyplněno jméno podnikatele.",
                nameof(personName));
        }
    }

    /// <summary>
    /// Změní obchodní jméno dopravce.
    /// </summary>
    public void Rename(string name)
    {
        Name = DomainValidation.Required(name, nameof(name), NameMaxLength);
    }

    /// <summary>
    /// Změní daňové údaje dopravce.
    /// </summary>
    public void ChangeTaxDetails(string? dic, string? personName)
    {
        Dic = DomainValidation.Optional(dic, nameof(dic), DicMaxLength);
        PersonName = DomainValidation.Optional(personName, nameof(personName), PersonNameMaxLength);

        if (LegalForm == AgencyLegalForm.NaturalPerson && PersonName is null)
        {
            throw new ArgumentException(
                "U fyzické osoby musí být vyplněno jméno podnikatele.",
                nameof(personName));
        }
    }
}
