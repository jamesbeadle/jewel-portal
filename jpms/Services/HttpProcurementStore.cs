using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpProcurementStore : IProcurementStore
{
    private readonly HttpClient httpClient;
    private IReadOnlyList<BidPackage> cachedPackages = Array.Empty<BidPackage>();
    private IReadOnlyList<WorkOrder> cachedWorkOrders = Array.Empty<WorkOrder>();
    private bool hasLoadedPackages;
    private bool hasLoadedWorkOrders;
    private readonly Dictionary<string, IReadOnlyList<Quote>> quotesByPackage = new();

    public HttpProcurementStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<BidPackage> PackagesFor(string projectId)
    {
        if (!hasLoadedPackages) _ = LoadPackagesAsync();
        return cachedPackages.Where(p => string.Equals(p.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }

    public BidPackage? FindPackage(string bidPackageId) =>
        cachedPackages.FirstOrDefault(p => string.Equals(p.BidPackageId, bidPackageId, StringComparison.OrdinalIgnoreCase));

    public BidPackage Upsert(BidPackage package)
    {
        _ = PostAsync("/api/bid-packages", package, isPackage: true);
        return package;
    }

    public IReadOnlyList<Quote> QuotesFor(string bidPackageId)
    {
        if (!quotesByPackage.ContainsKey(bidPackageId)) _ = LoadQuotesAsync(bidPackageId);
        return quotesByPackage.TryGetValue(bidPackageId, out var list) ? list : Array.Empty<Quote>();
    }

    public Quote SaveQuote(Quote quote)
    {
        _ = PostQuoteAsync(quote);
        return quote;
    }

    public IReadOnlyList<WorkOrder> AllWorkOrders()
    {
        if (!hasLoadedWorkOrders) _ = LoadWorkOrdersAsync();
        return cachedWorkOrders;
    }

    public IReadOnlyList<WorkOrder> WorkOrdersFor(string projectId) =>
        AllWorkOrders().Where(w => string.Equals(w.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();

    public WorkOrder? FindWorkOrder(string workOrderId) =>
        cachedWorkOrders.FirstOrDefault(w => string.Equals(w.WorkOrderId, workOrderId, StringComparison.OrdinalIgnoreCase));

    public WorkOrder Award(WorkOrder workOrder)
    {
        _ = PostAsync("/api/work-orders", workOrder, isPackage: false);
        return workOrder;
    }

    private async Task LoadPackagesAsync()
    {
        hasLoadedPackages = true;
        try { cachedPackages = (await httpClient.GetFromJsonAsync<List<BidPackage>>("/api/bid-packages"))?.AsReadOnly() ?? (IReadOnlyList<BidPackage>)Array.Empty<BidPackage>(); OnChange?.Invoke(); }
        catch { cachedPackages = Array.Empty<BidPackage>(); }
    }

    private async Task LoadWorkOrdersAsync()
    {
        hasLoadedWorkOrders = true;
        try { cachedWorkOrders = (await httpClient.GetFromJsonAsync<List<WorkOrder>>("/api/work-orders"))?.AsReadOnly() ?? (IReadOnlyList<WorkOrder>)Array.Empty<WorkOrder>(); OnChange?.Invoke(); }
        catch { cachedWorkOrders = Array.Empty<WorkOrder>(); }
    }

    private async Task LoadQuotesAsync(string bidPackageId)
    {
        try { quotesByPackage[bidPackageId] = (await httpClient.GetFromJsonAsync<List<Quote>>($"/api/bid-packages/{bidPackageId}/quotes"))?.AsReadOnly() ?? (IReadOnlyList<Quote>)Array.Empty<Quote>(); OnChange?.Invoke(); }
        catch { quotesByPackage[bidPackageId] = Array.Empty<Quote>(); }
    }

    private async Task PostAsync<T>(string url, T body, bool isPackage)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        if (isPackage) await LoadPackagesAsync();
        else await LoadWorkOrdersAsync();
    }

    private async Task PostQuoteAsync(Quote quote)
    {
        try { await httpClient.PostAsJsonAsync("/api/quotes", quote); } catch { return; }
        quotesByPackage.Remove(quote.BidPackageId);
        await LoadQuotesAsync(quote.BidPackageId);
    }
}
