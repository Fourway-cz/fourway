namespace Fourway.Core.Models;

/// <summary>
/// Jeden úsek cesty: přímá jízda jedním spojem mezi dvěma zastávkami.
/// </summary>
/// <remarks>
/// Doménový objekt vyhledávání spojů, nemaže se do databáze. V budoucnu se
/// může spojení skládat z více úseků a reprezentovat přestupy.
/// </remarks>
public sealed class Leg
{
    /// <summary>
    /// Spoj, kterým se úsek ujede.
    /// </summary>
    public Trip Trip { get; }

    /// <summary>
    /// Linka spoje.
    /// </summary>
    public Route Route { get; }

    /// <summary>
    /// Informace o lince z pohledu systémů integrované dopravy.
    /// </summary>
    public IReadOnlyList<RouteIdsInfo> IdsInfos { get; }

    /// <summary>
    /// Všechny zastávky spoje seřazené podle pořadí.
    /// </summary>
    public IReadOnlyList<LegStop> Stops { get; }

    /// <summary>
    /// Zastávka, na které se do spoje nastupuje.
    /// </summary>
    public Stop FromStop { get; }

    /// <summary>
    /// Zastávka, na které se ze spoje vystupuje.
    /// </summary>
    public Stop ToStop { get; }

    /// <summary>
    /// Absolutní čas odjezdu z <see cref="FromStop"/>.
    /// </summary>
    public DateTime DepartureTime { get; }

    /// <summary>
    /// Absolutní čas příjezdu do <see cref="ToStop"/>.
    /// </summary>
    public DateTime ArrivalTime { get; }

    /// <summary>
    /// Doba jízdy úsekem.
    /// </summary>
    public TimeSpan Duration => ArrivalTime - DepartureTime;

    /// <summary>
    /// Vytvoří nový úsek spoje.
    /// </summary>
    public Leg(
        Trip trip,
        Route route,
        IEnumerable<RouteIdsInfo> idsInfos,
        Stop fromStop,
        Stop toStop,
        IEnumerable<LegStop> stops,
        DateTime departureTime,
        DateTime arrivalTime)
    {
        Trip = trip ?? throw new ArgumentNullException(nameof(trip));
        Route = route ?? throw new ArgumentNullException(nameof(route));
        IdsInfos = idsInfos?.ToList() ?? throw new ArgumentNullException(nameof(idsInfos));
        Stops = stops?.ToList() ?? throw new ArgumentNullException(nameof(stops));
        FromStop = fromStop ?? throw new ArgumentNullException(nameof(fromStop));
        ToStop = toStop ?? throw new ArgumentNullException(nameof(toStop));

        if (Stops.Count == 0)
        {
            throw new ArgumentException("Úsek spoje musí obsahovat alespoň jednu zastávku.", nameof(stops));
        }

        if (arrivalTime <= departureTime)
        {
            throw new ArgumentException(
                "Čas příjezdu musí být po čase odjezdu.",
                nameof(arrivalTime));
        }

        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
    }
}
