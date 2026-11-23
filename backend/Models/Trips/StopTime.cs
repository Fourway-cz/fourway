namespace Fourway.Api.Models;

/// <summary>
/// Zastávka konkrétního spoje s časy příjezdu a odjezdu.
/// </summary>
/// <remarks>
/// Odpovídá záznamu v <c>Zasspoje.txt</c>. Časy jsou intervaly od půlnoci, takže
/// mohou přesáhnout 24 hodin. <see cref="StopId"/> je denormalizovaná hodnota
/// z <see cref="RouteStop"/> pro rychlejší vyhledávání.
/// </remarks>
public sealed class StopTime
{
    /// <summary>Maximální délka označení stanoviště.</summary>
    public const int PlatformCodeMaxLength = 20;

    /// <summary>
    /// Spoj, ke kterému čas patří.
    /// </summary>
    public Guid TripId { get; private set; }

    /// <summary>
    /// Pořadí zastávky ve spoji z pole <c>TarifniCislo</c>.
    /// </summary>
    public int StopSequence { get; private set; }

    /// <summary>
    /// Zastávka na trase linky.
    /// </summary>
    public Guid RouteStopId { get; private set; }

    /// <summary>
    /// Kanonická zastávka, denormalizovaná z <see cref="RouteStop.StopId"/>.
    /// </summary>
    public Guid StopId { get; private set; }

    /// <summary>
    /// Označení označníku nebo stanoviště z pole <c>CisloStanoviste</c>.
    /// </summary>
    public string? PlatformCode { get; private set; }

    /// <summary>
    /// Čas příjezdu jako interval od půlnoci.
    /// </summary>
    /// <remarks>
    /// Může přesáhnout 24 hodin. <see langword="null"/> u výchozí zastávky spoje.
    /// </remarks>
    public TimeSpan? ArrivalTime { get; private set; }

    /// <summary>
    /// Čas odjezdu jako interval od půlnoci.
    /// </summary>
    /// <remarks>
    /// Může přesáhnout 24 hodin. <see langword="null"/> u konečné zastávky spoje.
    /// </remarks>
    public TimeSpan? DepartureTime { get; private set; }

    /// <summary>
    /// Maximální čas příjezdu pro toleranci, pokud ho JDF 1.11 uvádí.
    /// </summary>
    public TimeSpan? ArrivalTimeMax { get; private set; }

    /// <summary>
    /// Maximální čas odjezdu pro toleranci, pokud ho JDF 1.11 uvádí.
    /// </summary>
    public TimeSpan? DepartureTimeMax { get; private set; }

    /// <summary>
    /// Způsob obsluhy zastávky: zastavuje, nebo projíždí.
    /// </summary>
    public StopKind StopKind { get; private set; }

    /// <summary>
    /// Označuje zastávku na znamení podle pevného kódu <c>18</c>.
    /// </summary>
    public bool RequestStop { get; private set; }

    /// <summary>
    /// Označuje nástup předem podle pevného kódu <c>31</c>.
    /// </summary>
    public bool BoardFront { get; private set; }

    /// <summary>
    /// Tarifní kilometry od výchozí zastávky.
    /// </summary>
    public int? TariffKm { get; private set; }

    /// <summary>
    /// Skutečné kilometry od výchozí zastávky.
    /// </summary>
    public int? ActualKm { get; private set; }

    /// <summary>
    /// Označuje, zda je vyplněný čas příjezdu.
    /// </summary>
    public bool HasArrival => ArrivalTime is not null;

    /// <summary>
    /// Označuje, zda je vyplněný čas odjezdu.
    /// </summary>
    public bool HasDeparture => DepartureTime is not null;

    /// <summary>
    /// Označuje, zda spoj zastávkou pouze projíždí.
    /// </summary>
    public bool IsPassThrough => StopKind == StopKind.PassesThrough;

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private StopTime()
    {
    }

    /// <summary>
    /// Vytvoří novou zastávku spoje.
    /// </summary>
    public StopTime(
        Guid tripId,
        int stopSequence,
        Guid routeStopId,
        Guid stopId,
        TimeSpan? arrivalTime,
        TimeSpan? departureTime,
        TimeSpan? arrivalTimeMax = null,
        TimeSpan? departureTimeMax = null,
        StopKind stopKind = StopKind.Stops,
        string? platformCode = null,
        bool requestStop = false,
        bool boardFront = false,
        int? tariffKm = null,
        int? actualKm = null)
    {
        TripId = tripId;
        StopSequence = DomainValidation.Positive(stopSequence, nameof(stopSequence));
        RouteStopId = routeStopId;
        StopId = stopId;
        PlatformCode = DomainValidation.Optional(
            platformCode,
            nameof(platformCode),
            PlatformCodeMaxLength);
        ArrivalTime = DomainValidation.ValidateTime(arrivalTime, nameof(arrivalTime));
        DepartureTime = DomainValidation.ValidateTime(departureTime, nameof(departureTime));
        ArrivalTimeMax = DomainValidation.ValidateTime(arrivalTimeMax, nameof(arrivalTimeMax));
        DepartureTimeMax = DomainValidation.ValidateTime(departureTimeMax, nameof(departureTimeMax));
        StopKind = stopKind;
        RequestStop = requestStop;
        BoardFront = boardFront;
        TariffKm = tariffKm is null
            ? null
            : DomainValidation.NotNegative(tariffKm.Value, nameof(tariffKm));
        ActualKm = actualKm is null
            ? null
            : DomainValidation.NotNegative(actualKm.Value, nameof(actualKm));

        if (ArrivalTimeMax is not null && ArrivalTime is not null && ArrivalTimeMax < ArrivalTime)
        {
            throw new ArgumentException(
                "Maximální čas příjezdu nesmí být před časem příjezdu.",
                nameof(arrivalTimeMax));
        }

        if (DepartureTimeMax is not null && DepartureTime is not null && DepartureTimeMax < DepartureTime)
        {
            throw new ArgumentException(
                "Maximální čas odjezdu nesmí být před časem odjezdu.",
                nameof(departureTimeMax));
        }
    }
}
