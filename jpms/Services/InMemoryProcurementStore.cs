using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryProcurementStore : IProcurementStore
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";

    private readonly List<BidPackage> packages = new()
    {
        new("BP-001", "PRJ-001", "Groundworks package", "Groundworks", BidPackageStatus.Awarded,       DateTimeOffset.UtcNow.AddDays(-60), NigelEmail),
        new("BP-002", "PRJ-001", "Brickwork package",   "Masonry",     BidPackageStatus.QuotesReceived,DateTimeOffset.UtcNow.AddDays(-20), NigelEmail),
        new("BP-003", "PRJ-002", "Electrical 1st fix",  "Electrical",  BidPackageStatus.Inviting,     DateTimeOffset.UtcNow.AddDays(-7),  NigelEmail)
    };

    private readonly List<Quote> quotes = new()
    {
        new("QT-001", "BP-001", "SC-001",  48_500m, "Includes piling allowance",       DateTimeOffset.UtcNow.AddDays(-55), false),
        new("QT-002", "BP-002", "SC-002",  41_200m, "Materials at current spot price", DateTimeOffset.UtcNow.AddDays(-15), false),
        new("QT-003", "BP-002", "SC-001",  45_900m, "Alternate supplier route",        DateTimeOffset.UtcNow.AddDays(-12), false)
    };

    private readonly List<WorkOrder> workOrders = new()
    {
        new("WO-001", "PRJ-001", "BP-001", "SC-001", 48_500m, "Groundworks per BoQ + foundation reinforcement", DateTimeOffset.UtcNow.AddDays(-50), NigelEmail)
    };

    public event Action? OnChange;

    public IReadOnlyList<BidPackage> PackagesFor(string projectId) =>
        packages.Where(p => string.Equals(p.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreatedAt).ToList().AsReadOnly();

    public BidPackage? FindPackage(string bidPackageId) =>
        packages.FirstOrDefault(p => string.Equals(p.BidPackageId, bidPackageId, StringComparison.OrdinalIgnoreCase));

    public BidPackage Upsert(BidPackage package)
    {
        var existing = FindPackage(package.BidPackageId);
        if (existing is not null) packages.Remove(existing);
        packages.Add(package);
        OnChange?.Invoke();
        return package;
    }

    public IReadOnlyList<Quote> QuotesFor(string bidPackageId) =>
        quotes.Where(q => string.Equals(q.BidPackageId, bidPackageId, StringComparison.OrdinalIgnoreCase))
              .OrderBy(q => q.Value).ToList().AsReadOnly();

    public Quote SaveQuote(Quote quote)
    {
        var existing = quotes.FirstOrDefault(q => q.QuoteId == quote.QuoteId);
        if (existing is not null) quotes.Remove(existing);
        quotes.Add(quote);
        OnChange?.Invoke();
        return quote;
    }

    public IReadOnlyList<WorkOrder> AllWorkOrders() =>
        workOrders.OrderByDescending(w => w.AwardedAt).ToList().AsReadOnly();

    public IReadOnlyList<WorkOrder> WorkOrdersFor(string projectId) =>
        workOrders.Where(w => string.Equals(w.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
                  .OrderByDescending(w => w.AwardedAt).ToList().AsReadOnly();

    public WorkOrder? FindWorkOrder(string workOrderId) =>
        workOrders.FirstOrDefault(w => string.Equals(w.WorkOrderId, workOrderId, StringComparison.OrdinalIgnoreCase));

    public WorkOrder Award(WorkOrder workOrder)
    {
        var existing = FindWorkOrder(workOrder.WorkOrderId);
        if (existing is not null) workOrders.Remove(existing);
        workOrders.Add(workOrder);
        var package = FindPackage(workOrder.BidPackageId);
        if (package is not null) Upsert(package with { Status = BidPackageStatus.Awarded });
        OnChange?.Invoke();
        return workOrder;
    }
}
