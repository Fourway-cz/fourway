namespace Fourway.Api.Models;

/// <summary>
/// Typ výjimky v kalendáři spoje podle pole <c>TypCaskodu</c> v souboru <c>Caskody.txt</c>.
/// </summary>
public enum ServiceExceptionType
{
    /// <summary>
    /// V tento den spoj jede (JDF kód <c>2</c>).
    /// </summary>
    Runs,

    /// <summary>
    /// V tento den spoj nejede (JDF kód <c>3</c>).
    /// </summary>
    DoesNotRun
}
