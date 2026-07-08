using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

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

public sealed class WorkOrdersReadModel : IReadModelStore<IReadOnlyList<WorkOrder>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<WorkOrder>? Current { get; private set; }
    public event Action? OnChanged;

    public WorkOrdersReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListWorkOrders(), cancellationToken);
        OnChanged?.Invoke();
    }
}

// The Work Orders tab's store: every order on the project with lines and supplier names, keyed
// per project so navigating between projects keeps each one's cached view (stale-while-revalidate,
// per the front-end data-loading convention).
public sealed class ProjectWorkOrdersReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ProjectWorkOrderDetail>> ordersByProject = new();

    public ProjectWorkOrdersReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ProjectWorkOrderDetail> Current(string projectId) =>
        ordersByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProjectWorkOrderDetail>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        ordersByProject[projectId] = await queries.AskAsync(new ListProjectWorkOrders(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
