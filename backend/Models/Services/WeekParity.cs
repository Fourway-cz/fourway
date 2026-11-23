namespace Fourway.Api.Models;

/// <summary>
/// Parita týdne, ve kterém má kalendářní vzor platit.
/// </summary>
/// <remarks>
/// JDF rozlišuje liché a sudé týdny u typů časových kódů <c>5</c> a <c>6</c>.
/// Parita se počítá buď z ISO týdne, nebo od <see cref="ServicePattern.ParityAnchor"/>.
/// </remarks>
public enum WeekParity
{
    /// <summary>
    /// Vzor platí každý týden.
    /// </summary>
    All,

    /// <summary>
    /// Vzor platí pouze v lichých týdnech.
    /// </summary>
    Odd,

    /// <summary>
    /// Vzor platí pouze v sudých týdnech.
    /// </summary>
    Even
}
