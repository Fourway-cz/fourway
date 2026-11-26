namespace Fourway.Core.Models;

/// <summary>
/// Jedno odjetí spoje ze zastávky v konkrétním dni provozu.
/// </summary>
/// <remarks>
/// Doménový objekt vyhledávací služby, nemaže se do databáze. Představuje
/// jednu položku odjezdového tablo: spoj, jeho linku a absolutní čas odjezdu.
/// </remarks>
public sealed class Departure
{
    /// <summary>
    /// Spoj, který ze zastávky odjíždí.
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
    /// Čas spoje na dotazované zastávce.
    /// </summary>
    public StopTime StopTime { get; }

    /// <summary>
    /// Zastávka, kam spoj končí (poslední zastávka spoje).
    /// </summary>
    public Stop Destination { get; }

    /// <summary>
    /// Absolutní čas odjezdu ze zastávky.
    /// </summary>
    public DateTime DepartureTime { get; }

    /// <summary>
    /// Vytvoří nové odjetí spoje ze zastávky.
    /// </summary>
    public Departure(
        Trip trip,
        Route route,
        IEnumerable<RouteIdsInfo> idsInfos,
        StopTime stopTime,
        Stop destination,
        DateTime departureTime)
    {
        Trip = trip ?? throw new ArgumentNullException(nameof(trip));
        Route = route ?? throw new ArgumentNullException(nameof(route));
        IdsInfos = idsInfos?.ToList() ?? throw new ArgumentNullException(nameof(idsInfos));
        StopTime = stopTime ?? throw new ArgumentNullException(nameof(stopTime));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        DepartureTime = departureTime;
    }
}
