using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Portal;

/// <summary>The signed-in subcontractor's own record. The API scopes the query to the session's
/// SubcontractorId, so there is nothing to key by — one record per signed-in portal user.</summary>
public sealed class PortalReadModel : IReadModelStore<SubcontractorPortalRecord>
{
    private readonly IQueryClient queries;
    public SubcontractorPortalRecord? Current { get; private set; }
    public event Action? OnChanged;

    public PortalReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new GetMyPortalRecord(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>The signed-in subcontractor's variation requests (server-scoped, like PortalReadModel).</summary>
public sealed class PortalVariationRequestsReadModel : IReadModelStore<IReadOnlyList<SubcontractorVariationRequest>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<SubcontractorVariationRequest>? Current { get; private set; }
    public event Action? OnChanged;

    public PortalVariationRequestsReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListMyVariationRequests(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>The signed-in subcontractor's issued work orders (server-scoped, like PortalReadModel).</summary>
public sealed class PortalWorkOrdersReadModel : IReadModelStore<IReadOnlyList<PortalWorkOrder>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<PortalWorkOrder>? Current { get; private set; }
    public event Action? OnChanged;

    public PortalWorkOrdersReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListMyWorkOrders(), cancellationToken);
        OnChanged?.Invoke();
    }
}
