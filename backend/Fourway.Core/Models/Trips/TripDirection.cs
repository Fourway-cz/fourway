namespace Fourway.Core.Models;

/// <summary>
/// Parita čísla spoje, která v JDF rozlišuje směr jízdy.
/// </summary>
/// <remarks>
/// Nejde o obecný směr typu tam/zpět, ale o konvenci JDF: liché číslo spoje
/// znamená jeden směr, sudé číslo druhý. Hodnota se neukládá, počítá se
/// z <see cref="Trip.TripNumber"/>.
/// </remarks>
public enum TripDirection
{
    /// <summary>
    /// Liché číslo spoje.
    /// </summary>
    Odd,

    /// <summary>
    /// Sudé číslo spoje.
    /// </summary>
    Even
}
