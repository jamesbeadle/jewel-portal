using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IProcurementStore
{
    IReadOnlyList<BidPackage> PackagesFor(string projectId);
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
