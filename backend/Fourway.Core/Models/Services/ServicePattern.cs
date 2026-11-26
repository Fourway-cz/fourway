namespace Fourway.Core.Models;

/// <summary>
/// Pravidelný vzor kalendáře: které dny v týdnu a v jakém období spoj jede.
/// </summary>
/// <remarks>
/// Jeden <see cref="Service"/> může mít více vzorů. Jejich platnosti se sjednocují,
/// takže lze popsat například liché týdny v jednom období a každý týden v jiném.
/// </remarks>
public sealed class ServicePattern
{
    /// <summary>Identifikátor vzoru.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Kalendář, ke kterému vzor patří.
    /// </summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Označuje, zda vzor platí v pondělí.</summary>
    public bool Monday { get; private set; }

    /// <summary>Označuje, zda vzor platí v úterý.</summary>
    public bool Tuesday { get; private set; }

    /// <summary>Označuje, zda vzor platí ve středu.</summary>
    public bool Wednesday { get; private set; }

    /// <summary>Označuje, zda vzor platí ve čtvrtek.</summary>
    public bool Thursday { get; private set; }

    /// <summary>Označuje, zda vzor platí v pátek.</summary>
    public bool Friday { get; private set; }

    /// <summary>Označuje, zda vzor platí v sobotu.</summary>
    public bool Saturday { get; private set; }

    /// <summary>Označuje, zda vzor platí v neděli.</summary>
    public bool Sunday { get; private set; }

    /// <summary>
    /// Začátek platnosti vzoru.
    /// </summary>
    public DateOnly ValidFrom { get; private set; }

    /// <summary>
    /// Konec platnosti vzoru.
    /// </summary>
    public DateOnly ValidTo { get; private set; }

    /// <summary>
    /// Parita týdne, ve kterém vzor platí.
    /// </summary>
    public WeekParity WeekParity { get; private set; }

    /// <summary>
    /// Datum, od kterého se počítá parita týdnů.
    /// </summary>
    /// <remarks>
    /// Pokud je <see langword="null"/>, počítá se parita z ISO týdne.
    /// Pokud je vyplněné, týden obsahující toto datum je považován za lichý.
    /// </remarks>
    public DateOnly? ParityAnchor { get; private set; }

    /// <summary>
    /// Označuje, zda vzor obsahuje alespoň jeden den v týdnu.
    /// </summary>
    public bool HasAnyDay =>
        Monday || Tuesday || Wednesday || Thursday || Friday || Saturday || Sunday;

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private ServicePattern()
    {
    }

    /// <summary>
    /// Vytvoří nový pravidelný vzor.
    /// </summary>
    public ServicePattern(
        Guid serviceId,
        bool monday,
        bool tuesday,
        bool wednesday,
        bool thursday,
        bool friday,
        bool saturday,
        bool sunday,
        DateOnly validFrom,
        DateOnly validTo,
        WeekParity weekParity = WeekParity.All,
        DateOnly? parityAnchor = null)
    {
        Id = Guid.CreateVersion7();
        ServiceId = serviceId;
        Monday = monday;
        Tuesday = tuesday;
        Wednesday = wednesday;
        Thursday = thursday;
        Friday = friday;
        Saturday = saturday;
        Sunday = sunday;
        ValidFrom = DomainValidation.ValidateDateRange(validFrom, validTo, nameof(validTo));
        ValidTo = validTo;
        WeekParity = weekParity;
        ParityAnchor = parityAnchor;

        if (!HasAnyDay)
        {
            throw new ArgumentException("Vzor musí obsahovat alespoň jeden den v týdnu.");
        }
    }

    /// <summary>
    /// Ověří, zda vzor platí v daném dni.
    /// </summary>
    public bool Includes(DateOnly date)
    {
        if (date < ValidFrom || date > ValidTo)
        {
            return false;
        }

        var dayMatches = date.DayOfWeek switch
        {
            DayOfWeek.Monday => Monday,
            DayOfWeek.Tuesday => Tuesday,
            DayOfWeek.Wednesday => Wednesday,
            DayOfWeek.Thursday => Thursday,
            DayOfWeek.Friday => Friday,
            DayOfWeek.Saturday => Saturday,
            DayOfWeek.Sunday => Sunday,
            _ => false
        };

        return dayMatches && ParityMatches(date);
    }

    /// <summary>
    /// Ověří, zda datum spadá do požadované parity týdne.
    /// </summary>
    public bool ParityMatches(DateOnly date)
    {
        if (WeekParity == WeekParity.All)
        {
            return true;
        }

        var isOddWeek = ParityAnchor is null
            ? IsOddIsoWeek(date)
            : IsOddWeekFromAnchor(date, ParityAnchor.Value);

        return WeekParity == WeekParity.Odd ? isOddWeek : !isOddWeek;
    }

    private static bool IsOddIsoWeek(DateOnly date)
    {
        var week = System.Globalization.ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));
        return week % 2 == 1;
    }

    private static bool IsOddWeekFromAnchor(DateOnly date, DateOnly anchor)
    {
        var weeks = (date.DayNumber - anchor.DayNumber) / 7;
        return weeks % 2 == 0;
    }
}
