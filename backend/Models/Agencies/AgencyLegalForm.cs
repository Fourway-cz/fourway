namespace Fourway.Api.Models;

/// <summary>
/// Právní forma dopravce podle pole <c>DruhDopravce</c> v souboru <c>Dopravci.txt</c>.
/// </summary>
public enum AgencyLegalForm
{
    /// <summary>
    /// Právnická osoba (JDF kód <c>1</c>).
    /// </summary>
    LegalPerson,

    /// <summary>
    /// Fyzická osoba (JDF kód <c>2</c>).
    /// </summary>
    NaturalPerson
}
