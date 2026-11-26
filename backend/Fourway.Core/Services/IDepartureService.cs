using Fourway.Core.Models;

namespace Fourway.Core.Services;

/// <summary>
/// Vyhledává spoje odjíždějící ze zastávky.
/// </summary>
public interface IDepartureService
{
    /// <summary>
    /// Najde spoje, které ze zastávky <paramref name="stop"/> odjíždějí
    /// nejdříve v <paramref name="departureTime"/>.
    /// </summary>
    /// <param name="stop">Načtená doménová instance zastávky.</param>
    /// <param name="departureTime">Nejranější žádaný čas odjezdu.</param>
    /// <param name="limit">Maximální počet vrácených odjezdů.</param>
    /// <param name="ct">Token zrušení.</param>
    /// <returns>Odjezdy seřazené od nejdřívějšího, nejvýše <paramref name="limit"/> kusů.</returns>
    Task<IReadOnlyCollection<Departure>> SearchAsync(
        Stop stop,
        DateTime departureTime,
        int limit,
        CancellationToken ct = default);
}
