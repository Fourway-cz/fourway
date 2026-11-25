namespace Fourway.Core.Models;

/// <summary>
/// Zastávka jednoho úseku spoje s absolutním časem.
/// </summary>
/// <remarks>
/// Doménový objekt vyhledávání spojů, nemaže se do databáze. Časy jsou
/// absolutní <see cref="DateTime"/> spočítané ze dne provozu spoje a intervalu
/// z <see cref="StopTime"/>.
/// </remarks>
public sealed class LegStop
{
    /// <summary>
    /// Kanonická zastávka.
    /// </summary>
    public Stop Stop { get; }

    /// <summary>
    /// Čas Spoje na této zastávce, ze kterého se časy dopočítaly.
    /// </summary>
    public StopTime StopTime { get; }

    /// <summary>
    /// Absolutní čas příjezdu do zastávky, pokud je známý.
    /// </summary>
    public DateTime? ArrivalTime { get; }

    /// <summary>
    /// Absolutní čas odjezdu ze zastávky, pokud je známý.
    /// </summary>
    public DateTime? DepartureTime { get; }

    /// <summary>
    /// Vytvoří zastávku úseku spoje.
    /// </summary>
    public LegStop(
        Stop stop,
        StopTime stopTime,
        DateTime? arrivalTime,
        DateTime? departureTime)
    {
        Stop = stop ?? throw new ArgumentNullException(nameof(stop));
        StopTime = stopTime ?? throw new ArgumentNullException(nameof(stopTime));

        if (Stop.Id != StopTime.StopId)
        {
            throw new ArgumentException(
                "Zastávka neodpovídá zastávce v čase spoje.",
                nameof(stop));
        }

        if (arrivalTime is not null && departureTime is not null && departureTime < arrivalTime)
        {
            throw new ArgumentException(
                "Čas odjezdu nesmí být před časem příjezdu.",
                nameof(departureTime));
        }

        ArrivalTime = arrivalTime;
        DepartureTime = departureTime;
    }
}
