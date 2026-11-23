namespace Fourway.Api.Models;

/// <summary>
/// Kmenová data linky.
/// </summary>
/// <remarks>
/// Odpovídá záznamu v <c>Linky.txt</c>. Linka je identifikována kombinací
/// <see cref="LicenseNumber"/>, <see cref="Variant"/> a <see cref="ValidFrom"/>.
/// </remarks>
public sealed class Route
{
    /// <summary>Maximální délka licenčního čísla linky.</summary>
    public const int LicenseNumberMaxLength = 10;

    /// <summary>Maximální délka názvu linky.</summary>
    public const int NameMaxLength = 300;

    /// <summary>Maximální délka čísla licence.</summary>
    public const int LicenseRefMaxLength = 100;

    /// <summary>Maximální délka názvu schvalujícího orgánu.</summary>
    public const int ApprovingAuthorityMaxLength = 200;

    /// <summary>Identifikátor linky.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Celostátní licenční číslo linky z pole <c>CisloLinky</c>.
    /// </summary>
    public string LicenseNumber { get; private set; } = null!;

    /// <summary>
    /// Rozlišení varianty linky z pole <c>RozliseniLinky</c>.
    /// </summary>
    public short Variant { get; private set; }

    /// <summary>
    /// Začátek platnosti jízdního řádu linky.
    /// </summary>
    public DateOnly ValidFrom { get; private set; }

    /// <summary>
    /// Konec platnosti jízdního řádu linky, pokud je známý.
    /// </summary>
    public DateOnly? ValidTo { get; private set; }

    /// <summary>
    /// Dávka, ze které linka pochází.
    /// </summary>
    public Guid ImportBatchId { get; private set; }

    /// <summary>
    /// Hlavní licencovaný dopravce linky.
    /// </summary>
    public Guid AgencyId { get; private set; }

    /// <summary>
    /// Textový popis trasy linky z pole <c>NazevLinky</c>.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Druh linky.
    /// </summary>
    public RouteLineType LineType { get; private set; }

    /// <summary>
    /// Druh dopravního prostředku.
    /// </summary>
    public VehicleMode VehicleMode { get; private set; }

    /// <summary>
    /// GTFS route type, pokud ho lze odvodit nebo doplnit.
    /// </summary>
    /// <remarks>
    /// JDF tento údaj neobsahuje, proto je volitelný.
    /// </remarks>
    public short? GtfsRouteType { get; private set; }

    /// <summary>
    /// Označuje jednosměrnou nebo okružní linku.
    /// </summary>
    public bool OneWay { get; private set; }

    /// <summary>
    /// Číslo licence nebo správního rozhodnutí.
    /// </summary>
    public string? LicenseRef { get; private set; }

    /// <summary>
    /// Začátek platnosti licence.
    /// </summary>
    public DateOnly? LicenseValidFrom { get; private set; }

    /// <summary>
    /// Konec platnosti licence.
    /// </summary>
    public DateOnly? LicenseValidTo { get; private set; }

    /// <summary>
    /// Schvalující úřad, například krajský úřad.
    /// </summary>
    public string? ApprovingAuthority { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private Route()
    {
    }

    /// <summary>
    /// Vytvoří novou linku.
    /// </summary>
    public Route(
        string licenseNumber,
        short variant,
        DateOnly validFrom,
        DateOnly? validTo,
        Guid importBatchId,
        Guid agencyId,
        string name,
        RouteLineType lineType,
        VehicleMode vehicleMode,
        short? gtfsRouteType = null,
        bool oneWay = false,
        string? licenseRef = null,
        DateOnly? licenseValidFrom = null,
        DateOnly? licenseValidTo = null,
        string? approvingAuthority = null)
    {
        Id = Guid.CreateVersion7();
        LicenseNumber = DomainValidation.Required(
            licenseNumber,
            nameof(licenseNumber),
            LicenseNumberMaxLength);
        Variant = DomainValidation.NotNegative(variant, nameof(variant));
        ValidFrom = DomainValidation.ValidateDateRange(validFrom, validTo, nameof(validTo));
        ValidTo = validTo;
        ImportBatchId = importBatchId;
        AgencyId = agencyId;
        Name = DomainValidation.Required(name, nameof(name), NameMaxLength);
        LineType = lineType;
        VehicleMode = vehicleMode;
        GtfsRouteType = gtfsRouteType is null
            ? null
            : DomainValidation.NotNegative(gtfsRouteType.Value, nameof(gtfsRouteType));
        OneWay = oneWay;
        LicenseRef = DomainValidation.Optional(licenseRef, nameof(licenseRef), LicenseRefMaxLength);
        LicenseValidFrom = licenseValidFrom;
        LicenseValidTo = licenseValidTo;
        ApprovingAuthority = DomainValidation.Optional(
            approvingAuthority,
            nameof(approvingAuthority),
            ApprovingAuthorityMaxLength);

        if (LicenseValidFrom is not null && LicenseValidTo is not null && LicenseValidTo < LicenseValidFrom)
        {
            throw new ArgumentException(
                "Konec platnosti licence nesmí být před začátkem.",
                nameof(licenseValidTo));
        }
    }

    /// <summary>
    /// Ověří, zda linka platí v daném dni.
    /// </summary>
    public bool IsValidOn(DateOnly date) =>
        date >= ValidFrom && (ValidTo is null || date <= ValidTo);

    /// <summary>
    /// Změní platnost jízdního řádu linky.
    /// </summary>
    public void ChangeValidity(DateOnly validFrom, DateOnly? validTo)
    {
        ValidFrom = DomainValidation.ValidateDateRange(validFrom, validTo, nameof(validTo));
        ValidTo = validTo;
    }

    /// <summary>
    /// Doplní nebo změní GTFS route type.
    /// </summary>
    public void SetGtfsRouteType(short? gtfsRouteType)
    {
        GtfsRouteType = gtfsRouteType is null
            ? null
            : DomainValidation.NotNegative(gtfsRouteType.Value, nameof(gtfsRouteType));
    }
}
