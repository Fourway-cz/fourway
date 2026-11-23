using NpgsqlTypes;

namespace Fourway.Api.Models;

/// <summary>
/// Druh linky podle pole <c>TypLinky</c> v souboru <c>Linky.txt</c>.
/// </summary>
public enum RouteLineType
{
    /// <summary>
    /// Vnitrokrajská linka (JDF kód <c>V</c>).
    /// </summary>
    [PgName("V")]
    Regional,

    /// <summary>
    /// Městská hromadná doprava (JDF kód <c>A</c>).
    /// </summary>
    [PgName("A")]
    Urban,

    /// <summary>
    /// Dálková linka (JDF kód <c>D</c>).
    /// </summary>
    [PgName("D")]
    LongDistance,

    /// <summary>
    /// Mezinárodní linka (JDF kód <c>M</c>).
    /// </summary>
    [PgName("M")]
    International,

    /// <summary>
    /// Zvláštní mezinárodní linka (JDF kód <c>Z</c>).
    /// </summary>
    [PgName("Z")]
    InternationalSpecial,

    /// <summary>
    /// Příměstská linka (JDF kód <c>P</c>).
    /// </summary>
    [PgName("P")]
    Suburban,

    /// <summary>
    /// Zvláštní nebo neveřejná linka (JDF kód <c>N</c>).
    /// </summary>
    [PgName("N")]
    Special
}
