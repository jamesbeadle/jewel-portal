using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IProcurementStore
{
    IReadOnlyList<BidPackage> PackagesFor(string projectId);

    /// <summary>Starts a background refetch of the project's bid packages even if cached.
    /// Call on page entry so navigating back to the tab shows fresh data
    /// (stale-while-revalidate).</summary>
    void Refresh(string projectId);
    Task<BidPackage?> FindPackageAsync(string bidPackageId);
    BidPackage Upsert(BidPackage package);

    IReadOnlyList<Quote> QuotesFor(string bidPackageId);
    Quote SaveQuote(Quote quote);

    IReadOnlyList<WorkOrder> AllWorkOrders();
    IReadOnlyList<WorkOrder> WorkOrdersFor(string projectId);
    WorkOrder? FindWorkOrder(string workOrderId);
    WorkOrder Award(WorkOrder workOrder);

    event Action? OnChange;
}
