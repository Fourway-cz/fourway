namespace Fourway.Api.Models;

/// <summary>
/// Zdroj poznámky k kalendáři spoje.
/// </summary>
public enum ServiceNoteSource
{
    /// <summary>
    /// Poznámka z časového kódu v <c>Caskody.txt</c>.
    /// </summary>
    CalendarCode,

    /// <summary>
    /// Obecná poznámka k lince v <c>Udaje.txt</c>.
    /// </summary>
    RouteNote,
}
