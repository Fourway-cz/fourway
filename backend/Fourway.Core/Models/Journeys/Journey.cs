namespace Fourway.Core.Models;

/// <summary>
/// Najdené spojení mezi výchozí a cílovou zastávkou.
/// </summary>
/// <remarks>
/// Doménový objekt vyhledávací služby, nemaže se do databáze. Slouží jako
/// výstup pro API a v budoucnu i pro cache. Spojení se skládá z jednoho nebo
/// více <see cref="Leg"/> úseků, takže může reprezentovat i přestupy.
/// </remarks>
public sealed class Journey
{
    /// <summary>
    /// Výchozí zastávka spojení.
    /// </summary>
    public Stop FromStop { get; }

    /// <summary>
    /// Cílová zastávka spojení.
    /// </summary>
    public Stop ToStop { get; }

    /// <summary>
    /// Úseky cesty ve správném pořadí.
    /// </summary>
    public IReadOnlyList<Leg> Legs { get; }

    /// <summary>
    /// Absolutní čas odjezdu z <see cref="FromStop"/>.
    /// </summary>
    public DateTime DepartureTime { get; }

    /// <summary>
    /// Absolutní čas příjezdu do <see cref="ToStop"/>.
    /// </summary>
    public DateTime ArrivalTime { get; }

    /// <summary>
    /// Celková doba cestování včetně případných přestupů.
    /// </summary>
    public TimeSpan Duration => ArrivalTime - DepartureTime;

    /// <summary>
    /// Vytvoří nové spojení.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Pokud úseky nenavazují na sebe, nejsou chronologické, nebo nezačínají
    /// ve výchozí a nekončí v cílové zastávce.
    /// </exception>
    public Journey(Stop fromStop, Stop toStop, IEnumerable<Leg> legs)
    {
        FromStop = fromStop ?? throw new ArgumentNullException(nameof(fromStop));
        ToStop = toStop ?? throw new ArgumentNullException(nameof(toStop));

        var legList = legs?.ToList() ?? throw new ArgumentNullException(nameof(legs));
        if (legList.Count == 0)
        {
            throw new ArgumentException("Spojení musí obsahovat alespoň jeden úsek.", nameof(legs));
        }

        if (legList[0].FromStop.Id != FromStop.Id || legList[^1].ToStop.Id != ToStop.Id)
        {
            throw new ArgumentException(
                "Úseky spoje musí začínat ve výchozí a končit v cílové zastávce.",
                nameof(legs));
        }

        for (var i = 1; i < legList.Count; i++)
        {
            if (legList[i].FromStop.Id != legList[i - 1].ToStop.Id)
            {
                throw new ArgumentException("Úseky spoje musí na sebe navazovat.", nameof(legs));
            }

            if (legList[i].DepartureTime < legList[i - 1].ArrivalTime)
            {
                throw new ArgumentException("Časy úseků spoje musí být chronologické.", nameof(legs));
            }
        }

        Legs = legList;
        DepartureTime = legList[0].DepartureTime;
        ArrivalTime = legList[^1].ArrivalTime;
    }
}
