using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpProcurementStore : IProcurementStore
{
    private readonly BidPackagesReadModel packagesReadModel;
    private readonly ProjectWorkOrdersReadModel workOrdersReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Projects whose packages have had a load started — prevents an empty result
    // from re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> packagesRequested = new();

    // Same guard for work orders, which are now fetched per project rather than company-wide.
    private readonly HashSet<string> workOrdersRequested = new();

    // Quotes per bid package, cached so render-time reads never block on async
    // (which deadlocks on WebAssembly). Saving a quote invalidates its package.
    private readonly AsyncQueryCache<string, IReadOnlyList<Quote>> quotes;

    public HttpProcurementStore(BidPackagesReadModel packagesReadModel, ProjectWorkOrdersReadModel workOrdersReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.packagesReadModel = packagesReadModel;
        this.workOrdersReadModel = workOrdersReadModel;
        this.queries = queries;
        this.commands = commands;
        packagesReadModel.OnChanged += () => OnChange?.Invoke();
        workOrdersReadModel.OnChanged += () => OnChange?.Invoke();
        quotes = new((id, ct) => queries.AskAsync(new ListQuotesForBidPackage(id), ct), () => OnChange?.Invoke());
    }

    public event Action? OnChange;

    public IReadOnlyList<BidPackage> PackagesFor(string projectId)
    {
        if (packagesRequested.Add(projectId)) _ = LoadPackagesAsync(projectId);
        return packagesReadModel.Current(projectId);
    }

    private async Task LoadPackagesAsync(string projectId)
    {
        try { await packagesReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { packagesRequested.Remove(projectId); }
    }

    // Forces a background reload of the project's bid packages even when cached. Pages call
    // this once on entry (never from render) so tab navigation picks up changes made elsewhere.
    public void Refresh(string projectId)
    {
        packagesRequested.Add(projectId);
        _ = LoadPackagesAsync(projectId);
        workOrdersRequested.Add(projectId);
        _ = LoadWorkOrdersAsync(projectId);
    }

    public Task<BidPackage?> FindPackageAsync(string bidPackageId) =>
        queries.AskAsync(new GetBidPackageById(bidPackageId), CancellationToken.None);

    public BidPackage Upsert(BidPackage package)
    {
        if (string.IsNullOrEmpty(package.BidPackageId))
            _ = CreatePackageAsync(package);
        else _ = UpdatePackageAsync(package);
        return package;
    }

    public IReadOnlyList<Quote> QuotesFor(string bidPackageId) =>
        quotes.Get(bidPackageId, Array.Empty<Quote>());

    public Quote SaveQuote(Quote quote)
    {
        _ = SaveQuoteAsync(quote);
        return quote;
    }

    private async Task SaveQuoteAsync(Quote quote)
    {
        if (string.IsNullOrEmpty(quote.QuoteId))
            await commands.SendAsync(new SubmitQuoteForBidPackage(quote.BidPackageId, quote.SubcontractorId, quote.Value, quote.Notes), CancellationToken.None);
        else
            await commands.SendAsync(new ReviseQuote(quote.QuoteId, quote.Value, quote.Notes), CancellationToken.None);
        quotes.Invalidate(quote.BidPackageId);
    }

    // Work orders are read per project, sharing the Work Orders tab's cache. This used to call a
    // company-wide /api/work-orders and filter the result down in the browser, so opening one
    // project's Requests tab pulled every work order in the business over the wire (and scanned
    // the whole table server-side).
    public IReadOnlyList<WorkOrder> WorkOrdersFor(string projectId)
    {
        if (workOrdersRequested.Add(projectId)) _ = LoadWorkOrdersAsync(projectId);
        return workOrdersReadModel.Current(projectId).Select(detail => detail.Order).ToList().AsReadOnly();
    }

    private async Task LoadWorkOrdersAsync(string projectId)
    {
        try { await workOrdersReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { workOrdersRequested.Remove(projectId); }
    }

    public WorkOrder Award(WorkOrder workOrder)
    {
        _ = AwardAsync(workOrder);
        return workOrder;
    }

    private async Task CreatePackageAsync(BidPackage package)
    {
        await commands.SendAsync(new CreateBidPackage(package.ProjectId, package.Title, package.Trade, package.OwnerEmail, package.MaterialsApplicable), CancellationToken.None);
        await packagesReadModel.RefreshAsync(package.ProjectId, CancellationToken.None);
    }

    private async Task UpdatePackageAsync(BidPackage package)
    {
        await commands.SendAsync(new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade, package.Status, package.OwnerEmail, package.MaterialsApplicable), CancellationToken.None);
        await packagesReadModel.RefreshAsync(package.ProjectId, CancellationToken.None);
    }

    private async Task AwardAsync(WorkOrder workOrder)
    {
        await commands.SendAsync(new AwardBidPackage(workOrder.BidPackageId, workOrder.ProjectId, workOrder.SubcontractorId, workOrder.Value, workOrder.Scope, workOrder.AwardedByEmail), CancellationToken.None);
        workOrdersRequested.Add(workOrder.ProjectId);
        await workOrdersReadModel.RefreshAsync(workOrder.ProjectId, CancellationToken.None);
    }
}
