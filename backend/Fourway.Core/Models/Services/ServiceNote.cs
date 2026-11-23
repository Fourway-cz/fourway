namespace Fourway.Core.Models;

/// <summary>
/// Textová poznámka k kalendáři spoje.
/// </summary>
/// <remarks>
/// Ukládá textové poznámky z <c>Caskody.txt</c> (TextPoznamky) a <c>Udaje.txt</c>
/// (TextUdaje). Poznámky mohou obsahovat informace o tarifech, SPP, návaznostech
/// na jiné linky, kontaktech na dispečink atd.
/// </remarks>
public sealed class ServiceNote
{
    /// <summary>Maximální délka textu poznámky.</summary>
    public const int TextMaxLength = 1000;

    /// <summary>Maximální délka označení poznámky.</summary>
    public const int LabelMaxLength = 50;

    /// <summary>Identifikátor poznámky.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Kalendář, ke kterému poznámka patří.
    /// </summary>
    public Guid ServiceId { get; private set; }

    /// <summary>
    /// Pořadové číslo poznámky pro řazení.
    /// </summary>
    public int Sequence { get; private set; }

    /// <summary>
    /// Označení poznámky z pole <c>OznaceniCaskodu</c>, například <c>"42"</c>, <c>"10"</c>, <c>"p"</c>.
    /// </summary>
    /// <remarks>
    /// Volitelné; vyplněné pouze u poznámek z <c>Caskody.txt</c>.
    /// </remarks>
    public string? Label { get; private set; }

    /// <summary>
    /// Text poznámky.
    /// </summary>
    /// <remarks>
    /// Z <c>TextPoznamky</c> v <c>Caskody.txt</c> nebo <c>TextUdaje</c> v <c>Udaje.txt</c>.
    /// </remarks>
    public string Text { get; private set; } = null!;

    /// <summary>
    /// Typ zdroje poznámky.
    /// </summary>
    public ServiceNoteSource Source { get; private set; }

    /// <summary>
    /// Začátek platnosti poznámky, pokud je časově omezená.
    /// </summary>
    public DateOnly? ValidFrom { get; private set; }

    /// <summary>
    /// Konec platnosti poznámky, pokud je časově omezená.
    /// </summary>
    public DateOnly? ValidTo { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private ServiceNote()
    {
    }

    /// <summary>
    /// Vytvoří novou poznámku k kalendáři.
    /// </summary>
    /// <param name="serviceId">Identifikátor kalendáře.</param>
    /// <param name="sequence">Pořadové číslo pro řazení.</param>
    /// <param name="label">Označení poznámky (volitelné).</param>
    /// <param name="text">Text poznámky.</param>
    /// <param name="source">Zdroj poznámky.</param>
    /// <param name="validFrom">Začátek platnosti (volitelné).</param>
    /// <param name="validTo">Konec platnosti (volitelné).</param>
    public ServiceNote(
        Guid serviceId,
        int sequence,
        string? label,
        string text,
        ServiceNoteSource source,
        DateOnly? validFrom = null,
        DateOnly? validTo = null)
    {
        Id = Guid.CreateVersion7();
        ServiceId = serviceId;
        Sequence = DomainValidation.NotNegative(sequence, nameof(sequence));
        Label = DomainValidation.Optional(label, nameof(label), LabelMaxLength);
        Text = DomainValidation.Required(text, nameof(text), TextMaxLength);
        Source = source;
        ValidFrom = DomainValidation.ValidateDateRange(
            validFrom ?? DateOnly.MinValue,
            validTo,
            nameof(validTo));
        if (validFrom is not null)
        {
            ValidFrom = validFrom;
        }
        ValidTo = validTo;
    }

    /// <summary>
    /// Ověří, zda poznámka platí v daném dni.
    /// </summary>
    public bool IsValidOn(DateOnly date)
    {
        if (ValidFrom is not null && date < ValidFrom)
        {
            return false;
        }

        if (ValidTo is not null && date > ValidTo)
        {
            return false;
        }

        return true;
    }
}
