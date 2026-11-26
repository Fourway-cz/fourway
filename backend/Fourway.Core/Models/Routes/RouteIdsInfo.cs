namespace Fourway.Core.Models;

/// <summary>
/// Informace o lince z pohledu konkrétního systému integrované dopravy.
/// </summary>
/// <remarks>
/// Odpovídá záznamu v <c>LinExt.txt</c>. Pro jednu linku a jeden systém
/// se očekává jeden záznam.
/// </remarks>
public sealed class RouteIdsInfo
{
    /// <summary>Maximální délka veřejného čísla linky.</summary>
    public const int PublicNumberMaxLength = 20;

    /// <summary>Identifikátor záznamu.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Linka, ke které se informace vztahuje.
    /// </summary>
    public Guid RouteId { get; private set; }

    /// <summary>
    /// Systém integrované dopravy.
    /// </summary>
    public Guid IdsSystemId { get; private set; }

    /// <summary>
    /// Veřejné číslo linky pro cestující z pole <c>OznaceniLinky</c>.
    /// </summary>
    public string PublicNumber { get; private set; } = null!;

    /// <summary>
    /// Označuje, zda je linka v systému plně integrovaná.
    /// </summary>
    public bool FullyIntegrated { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private RouteIdsInfo()
    {
    }

    /// <summary>
    /// Vytvoří novou informaci o lince v IDS.
    /// </summary>
    public RouteIdsInfo(
        Guid routeId,
        Guid idsSystemId,
        string publicNumber,
        bool fullyIntegrated)
    {
        Id = Guid.CreateVersion7();
        RouteId = routeId;
        IdsSystemId = idsSystemId;
        PublicNumber = DomainValidation.Required(
            publicNumber,
            nameof(publicNumber),
            PublicNumberMaxLength);
        FullyIntegrated = fullyIntegrated;
    }
}
