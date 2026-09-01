
namespace Jewel.JPMS.Features.Procurement;

public sealed class BidPackagesReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<BidPackage>> packagesByProject = new();

    public BidPackagesReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<BidPackage> Current(string projectId) =>
        packagesByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<BidPackage>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        packagesByProject[projectId] = await queries.AskAsync(new ListBidPackagesForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

// The Work Orders tab's store: every order on the project with lines and supplier names, keyed
// per project so navigating between projects keeps each one's cached view (stale-while-revalidate,
// per the front-end data-loading convention).
//
// This is also what backs IProcurementStore.WorkOrdersFor. There used to be a second, company-wide
// WorkOrdersReadModel over an unfiltered /api/work-orders; it was retired because its only consumer
// filtered the result down to one project in the browser, which meant opening any project's
// Requests tab downloaded every work order in the business and table-scanned the server side of it.
public sealed class ProjectWorkOrdersReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ProjectWorkOrderDetail>> ordersByProject = new();

    public ProjectWorkOrdersReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ProjectWorkOrderDetail> Current(string projectId) =>
        ordersByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProjectWorkOrderDetail>();

    /// <summary>True once this project's work orders have landed — Current(...) answers empty
    /// until then, which sums to a misleading zero.</summary>
    public bool LoadedFor(string projectId) => ordersByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        ordersByProject[projectId] = await queries.AskAsync(new ListProjectWorkOrders(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
