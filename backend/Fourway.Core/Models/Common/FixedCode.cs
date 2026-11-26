namespace Fourway.Core.Models;

/// <summary>
/// Pevné kódy z JDF specifikace.
/// </summary>
/// <remarks>
/// Odpovídá číselníku v <c>Pevnykod.txt</c>. Kódy plynují z legislativy a
/// neměly by se měnit. Používají se ve spojích, zastávkách a trasách linek.
/// </remarks>
public enum FixedCode
{
    /// <summary>
    /// Jede v pracovní dny (JDF kód <c>1</c>, symbol <c>X</c>).
    /// </summary>
    /// <remarks>
    /// Používá se v <c>Spoje.txt</c> (PevnyKod1–10).
    /// </remarks>
    Weekdays = 1,

    /// <summary>
    /// Neděle a svátky (JDF kód <c>2</c>, symbol <c>+</c>).
    /// </summary>
    /// <remarks>
    /// Používá se v <c>Spoje.txt</c> (PevnyKod1–10).
    /// </remarks>
    SundaysAndHolidays = 2,

    /// <summary>
    /// Bezbariérové vozidlo (JDF kód <c>14</c>, symbol <c>@</c>).
    /// </summary>
    /// <remarks>
    /// Používá se v <c>Spoje.txt</c> (PevnyKod1–10).
    /// </remarks>
    WheelchairAccessible = 14,

    /// <summary>
    /// Zastávka na znamení (JDF kód <c>18</c>, symbol <c>x</c>).
    /// </summary>
    /// <remarks>
    /// Používá se v <c>Zaslinky.txt</c> (PevnyKod1–4) a <c>Zasspoje.txt</c> (PevnyKodSpoje1–3).
    /// </remarks>
    RequestStop = 18,

    /// <summary>
    /// Cyklobus – možnost přepravy kol (JDF kód <c>21</c>, symbol <c>(</c>).
    /// </summary>
    /// <remarks>
    /// Používá se v <c>Spoje.txt</c> (PevnyKod1–10).
    /// </remarks>
    BikesAllowed = 21,

    /// <summary>
    /// Nástup předními dveřmi (JDF kód <c>31</c>).
    /// </summary>
    /// <remarks>
    /// Používá se v <c>Zasspoje.txt</c> (PevnyKodSpoje1–3).
    /// </remarks>
    BoardFront = 31,

    /// <summary>
    /// Vyloučena místní přeprava (JDF kód <c>30</c>, symbol <c>§</c>).
    /// </summary>
    /// <remarks>
    /// Používá se v <c>Spoje.txt</c> (PevnyKod1–10).
    /// </remarks>
    LocalTransportExcluded = 30,
}
