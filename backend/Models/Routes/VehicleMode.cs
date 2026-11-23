using NpgsqlTypes;

namespace Fourway.Api.Models;

/// <summary>
/// Druh dopravního prostředku podle pole <c>DopravniProstredek</c> v souboru <c>Linky.txt</c>.
/// </summary>
public enum VehicleMode
{
    /// <summary>
    /// Autobus (JDF kód <c>A</c>).
    /// </summary>
    [PgName("A")]
    Bus,

    /// <summary>
    /// Trolejbus (JDF kód <c>T</c>).
    /// </summary>
    [PgName("T")]
    Trolleybus,

    /// <summary>
    /// Tramvaj (JDF kód <c>E</c>).
    /// </summary>
    [PgName("E")]
    Tram,

    /// <summary>
    /// Vlak (JDF kód <c>V</c>).
    /// </summary>
    [PgName("V")]
    Train,

    /// <summary>
    /// Lanovka (JDF kód <c>L</c>).
    /// </summary>
    [PgName("L")]
    CableCar,

    /// <summary>
    /// Přívoz (JDF kód <c>P</c>).
    /// </summary>
    [PgName("P")]
    Ferry
}
