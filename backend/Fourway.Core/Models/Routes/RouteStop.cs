namespace Fourway.Core.Models;

/// <summary>
/// Zastávka na trase linky.
/// </summary>
/// <remarks>
/// Odpovídá záznamu v <c>Zaslinky.txt</c>. Pořadí je určeno polem
/// <c>TarifniCislo</c>, tarifní pásma jsou uložena jako surové hodnoty.
/// </remarks>
public sealed class RouteStop
{
    /// <summary>Identifikátor zastávky na trase.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Linka, na které zastávka leží.
    /// </summary>
    public Guid RouteId { get; private set; }

    /// <summary>
    /// Pořadové číslo zastávky na trase z pole <c>TarifniCislo</c>.
    /// </summary>
    public int Sequence { get; private set; }

    /// <summary>
    /// Kanonická zastávka.
    /// </summary>
    public Guid StopId { get; private set; }

    /// <summary>
    /// Tarifní pásma z pole <c>TarifniPasmo</c>, rozdělená na jednotlivé hodnoty.
    /// </summary>
    /// <remarks>
    /// JDF hodnoty jsou oddělené čárkou, například <c>121,122</c>. Pořadí se
    /// zachovává, duplicity se odstraňují. Zóny nelze spolehlivě mapovat na
    /// konkrétní IDS systém, proto zůstávají na úrovni zastávky na trase.
    /// </remarks>
    public string[] TariffZones { get; private set; } = [];

    /// <summary>
    /// Označuje zastávku na znamení na úrovni trasy podle pevného kódu <c>18</c>.
    /// </summary>
    /// <remarks>
    /// Extrahuje se z <c>PevnyKod1_4</c> v <c>Zaslinky.txt</c>.
    /// </remarks>
    public bool RequestStop { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private RouteStop()
    {
    }

    /// <summary>
    /// Vytvoří novou zastávku na trase.
    /// </summary>
    public RouteStop(
        Guid routeId,
        int sequence,
        Guid stopId,
        IEnumerable<string>? tariffZones = null,
        bool requestStop = false)
    {
        Id = Guid.CreateVersion7();
        RouteId = routeId;
        Sequence = DomainValidation.Positive(sequence, nameof(sequence));
        StopId = stopId;
        SetTariffZones(tariffZones);
        RequestStop = requestStop;
    }

    /// <summary>
    /// Nastaví tarifní pásma zastávky.
    /// </summary>
    public void SetTariffZones(IEnumerable<string>? tariffZones)
    {
        TariffZones = tariffZones?
            .Select(zone => zone?.Trim())
            .Where(zone => !string.IsNullOrWhiteSpace(zone))
            .Select(zone => zone!)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
    }
}
