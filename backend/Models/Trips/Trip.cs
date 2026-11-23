namespace Fourway.Api.Models;

/// <summary>
/// Konkrétní spoj linky.
/// </summary>
/// <remarks>
/// Odpovídá záznamu v <c>Spoje.txt</c>. Spoj je identifikován linkou a číslem
/// spoje; směr se odvozuje z parity čísla.
/// </remarks>
public sealed class Trip
{
    /// <summary>Identifikátor spoje.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Linka, ke které spoj patří.
    /// </summary>
    public Guid RouteId { get; private set; }

    /// <summary>
    /// Číslo spoje z pole <c>CisloSpoje</c>.
    /// </summary>
    public int TripNumber { get; private set; }

    /// <summary>
    /// Kalendář, který určuje dny provozu spoje.
    /// </summary>
    public Guid ServiceId { get; private set; }

    /// <summary>
    /// Označuje bezbariérové vozidlo podle pevného kódu <c>14</c>.
    /// </summary>
    /// <remarks>
    /// Nepřítomnost kódu v JDF znamená, že spoj bezbariérový není.
    /// </remarks>
    public bool WheelchairAccessible { get; private set; }

    /// <summary>
    /// Označuje možnost přepravy kol podle pevného kódu <c>21</c>.
    /// </summary>
    public bool BikesAllowed { get; private set; }

    /// <summary>
    /// Označuje, zda je u spoje vyloučena místní přeprava podle pevného kódu <c>30</c>.
    /// </summary>
    /// <remarks>
    /// Extrahuje se z <c>PevnyKod1_10</c> v <c>Spoje.txt</c>.
    /// </remarks>
    public bool LocalTransportExcluded { get; private set; }

    /// <summary>
    /// Parita čísla spoje, která v JDF rozlišuje směr.
    /// </summary>
    /// <remarks>
    /// Odvozená hodnota, neukládá se do databáze.
    /// </remarks>
    public TripDirection Direction =>
        TripNumber % 2 == 1 ? TripDirection.Odd : TripDirection.Even;

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private Trip()
    {
    }

    /// <summary>
    /// Vytvoří nový spoj.
    /// </summary>
    public Trip(
        Guid routeId,
        int tripNumber,
        Guid serviceId,
        bool wheelchairAccessible = false,
        bool bikesAllowed = false,
        bool localTransportExcluded = false)
    {
        Id = Guid.CreateVersion7();
        RouteId = routeId;
        TripNumber = DomainValidation.Positive(tripNumber, nameof(tripNumber));
        ServiceId = serviceId;
        WheelchairAccessible = wheelchairAccessible;
        BikesAllowed = bikesAllowed;
        LocalTransportExcluded = localTransportExcluded;
    }

    /// <summary>
    /// Nastaví příznaky dostupnosti vozidla.
    /// </summary>
    public void SetAccessibility(bool wheelchairAccessible, bool bikesAllowed, bool localTransportExcluded = false)
    {
        WheelchairAccessible = wheelchairAccessible;
        BikesAllowed = bikesAllowed;
        LocalTransportExcluded = localTransportExcluded;
    }
}
