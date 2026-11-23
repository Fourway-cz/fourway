namespace Fourway.Core.Models;

/// <summary>
/// Mapování interního čísla zastávky z konkrétní importní dávky na kanonickou zastávku.
/// </summary>
/// <remarks>
/// <c>CisloZastavky</c> je v JDF unikátní pouze v rámci jedné dávky, proto je
/// součástí primárního klíče spolu s <see cref="ImportBatchId"/>.
/// </remarks>
public sealed class StopSourceRef
{
    /// <summary>
    /// Dávka, ve které se zdrojové číslo vyskytlo.
    /// </summary>
    public Guid ImportBatchId { get; private set; }

    /// <summary>
    /// Číslo zastávky z pole <c>CisloZastavky</c> v dané dávce.
    /// </summary>
    public int SourceStopNo { get; private set; }

    /// <summary>
    /// Kanonická zastávka, na kterou se zdrojové číslo mapuje.
    /// </summary>
    public Guid StopId { get; private set; }

    // Pro EF Core. V doménovém kódu se nepoužívá.
    private StopSourceRef()
    {
    }

    /// <summary>
    /// Vytvoří nové mapování zdrojové zastávky.
    /// </summary>
    public StopSourceRef(Guid importBatchId, int sourceStopNo, Guid stopId)
    {
        ImportBatchId = importBatchId;
        SourceStopNo = DomainValidation.Positive(sourceStopNo, nameof(sourceStopNo));
        StopId = stopId;
    }
}
