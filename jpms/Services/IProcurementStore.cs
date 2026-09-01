
namespace Jewel.JPMS.Services;

public interface IProcurementStore
{
    IReadOnlyList<BidPackage> PackagesFor(string projectId);

    /// <summary>Starts a background refetch of the project's bid packages and work orders even if
    /// cached. Call on page entry so navigating back to the tab shows fresh data
    /// (stale-while-revalidate).</summary>
    void Refresh(string projectId);
    Task<BidPackage?> FindPackageAsync(string bidPackageId);
    BidPackage Upsert(BidPackage package);

    IReadOnlyList<Quote> QuotesFor(string bidPackageId);
    Quote SaveQuote(Quote quote);

    /// <summary>The project's work orders, from the same per-project cache the Work Orders tab
    /// fills. There is deliberately no company-wide accessor any more: the only caller was this
    /// one, and going through it made a single project page fetch every work order in the
    /// business.</summary>
    IReadOnlyList<WorkOrder> WorkOrdersFor(string projectId);
    WorkOrder Award(WorkOrder workOrder);

    event Action? OnChange;
}
