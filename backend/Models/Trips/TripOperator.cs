namespace Fourway.Api.Models;

/// <summary>
/// Dopravce zajišťující konkrétní spoj.
/// </summary>
/// <remarks>
/// Plní se z <c>Altdop.txt</c>, tedy pro alternativní dopravce. Hlavní dopravce
/// linky je na <see cref="Route.AgencyId"/> a záměrně se sem neduplikuje.
/// </remarks>
public sealed class TripOperator
{
    /// <summary>Identifikátor záznamu.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Spoj, který dopravce zajišťuje.
    /// </summary>
    public Guid TripId { get; private set; }

    /// <summary>
    /// Dopravce, který spoj zajišťuje.
    /// </summary>
    public Guid AgencyId { get; private set; }

    /// <summary>
    /// Začátek platnosti subdodávky, pokud je uvedený.
    /// </summary>
    public DateOnly? ValidFrom { get; private set; }

    /// <summary>
    /// Konec platnosti subdodávky, pokud je uvedený.
    /// </summary>
    public DateOnly? ValidTo { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private TripOperator()
    {
    }

    /// <summary>
    /// Vytvoří nové přiřazení dopravce ke spoji.
    /// </summary>
    public TripOperator(
        Guid tripId,
        Guid agencyId,
        DateOnly? validFrom = null,
        DateOnly? validTo = null)
    {
        Id = Guid.CreateVersion7();
        TripId = tripId;
        AgencyId = agencyId;
        ValidFrom = validFrom;
        ValidTo = validTo;

        if (validFrom is not null && validTo is not null && validTo < validFrom)
        {
            throw new ArgumentException(
                "Konec platnosti nesmí být před začátkem.",
                nameof(validTo));
        }
    }

    /// <summary>
    /// Ověří, zda přiřazení platí v daném dni.
    /// </summary>
    public bool IsValidOn(DateOnly date) =>
        (ValidFrom is null || date >= ValidFrom) &&
        (ValidTo is null || date <= ValidTo);
}
