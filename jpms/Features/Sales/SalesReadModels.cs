using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Features.Sales;

/// <summary>
/// The lead register (Sales → Leads, 2026-09-06): one company-wide list, newest-captured first;
/// the page filters by stage, strategy and text client-side. Nullable Current = no fetch has
/// landed yet (gate on it).
/// </summary>
public sealed class LeadListReadModel : IReadModelStore<IReadOnlyList<Lead>>
{
    private readonly IQueryClient queries;
    public LeadListReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<Lead>? Current { get; private set; }
    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListLeads(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>One lead with its timeline, keyed by lead id — the lead page's read. LoadedFor says
/// whether a fetch has landed for the key (a null value then means "no such lead").</summary>
public sealed class LeadDetailReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, LeadDetail?> byLead = new();
    public LeadDetailReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public LeadDetail? Current(string leadId) => byLead.TryGetValue(leadId, out var detail) ? detail : null;
    public bool LoadedFor(string leadId) => byLead.ContainsKey(leadId);

    public async Task RefreshAsync(string leadId, CancellationToken cancellationToken)
    {
        byLead[leadId] = await queries.AskAsync(new GetLead(leadId), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>The strategies with their funnels — the Strategies page and the lead form's
/// strategy picker. Nullable Current = not fetched yet.</summary>
public sealed class SalesStrategyListReadModel : IReadModelStore<IReadOnlyList<SalesStrategyOverview>>
{
    private readonly IQueryClient queries;
    public SalesStrategyListReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<SalesStrategyOverview>? Current { get; private set; }
    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListSalesStrategies(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>One strategy with its funnel and leads, keyed by strategy id — the strategy page.</summary>
public sealed class SalesStrategyDetailReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, SalesStrategyDetail?> byStrategy = new();
    public SalesStrategyDetailReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public SalesStrategyDetail? Current(string strategyId) => byStrategy.TryGetValue(strategyId, out var detail) ? detail : null;
    public bool LoadedFor(string strategyId) => byStrategy.ContainsKey(strategyId);

    public async Task RefreshAsync(string strategyId, CancellationToken cancellationToken)
    {
        byStrategy[strategyId] = await queries.AskAsync(new GetSalesStrategy(strategyId), cancellationToken);
        OnChanged?.Invoke();
    }
}
