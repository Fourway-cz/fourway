namespace Fourway.Api.Models;

/// <summary>
/// Způsob obsluhy zastávky spojem podle pole <c>Zasspoje.txt</c>.
/// </summary>
public enum StopKind
{
    /// <summary>
    /// Spoj v zastávce zastavuje (JDF hodnota <c>0</c>).
    /// </summary>
    Stops,

    /// <summary>
    /// Spoj zastávkou pouze projíždí bez zastavení (JDF symbol <c>|</c>).
    /// </summary>
    PassesThrough
}
