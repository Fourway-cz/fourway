using Fourway.Core.Models;

namespace Fourway.Core.Services;

/// <summary>
/// Vyhledává spojení mezi dvěma zastávkami.
/// </summary>
public interface IJourneySearchService
{
    /// <summary>
    /// Najde spojení z <paramref name="fromStop"/> do <paramref name="toStop"/>,
    /// která odjíždějí nejdříve v <paramref name="departureTime"/>.
    /// </summary>
    /// <param name="fromStop">Načtená doménová instance výchozí zastávky.</param>
    /// <param name="toStop">Načtená doménová instance cílové zastávky.</param>
    /// <param name="departureTime">Nejranější žádaný čas odjezdu.</param>
    /// <param name="ct">Token zrušení.</param>
    /// <returns>Seřazené spojení od nejdřívějšího odjezdu.</returns>
    Task<IReadOnlyCollection<Journey>> SearchAsync(
        Stop fromStop,
        Stop toStop,
        DateTime departureTime,
        CancellationToken ct = default);
}
