namespace Fourway.Api.Models;

/// <summary>
/// Konkrétní datová výjimka kalendáře.
/// </summary>
/// <remarks>
/// Používá se pro jednorázové odchylky, například státní svátek nebo mimořádné
/// posílení. Pravidelnost patří do <see cref="ServicePattern"/>, ne sem.
/// </remarks>
public sealed class ServiceException
{
    /// <summary>
    /// Kalendář, ke kterému výjimka patří.
    /// </summary>
    public Guid ServiceId { get; private set; }

    /// <summary>
    /// Datum, ke kterému se výjimka vztahuje.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Typ výjimky: spoj jede, nebo nejede.
    /// </summary>
    public ServiceExceptionType ExceptionType { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private ServiceException()
    {
    }

    /// <summary>
    /// Vytvoří novou výjimku kalendáře.
    /// </summary>
    public ServiceException(Guid serviceId, DateOnly date, ServiceExceptionType exceptionType)
    {
        ServiceId = serviceId;
        Date = date;
        ExceptionType = exceptionType;
    }
}
