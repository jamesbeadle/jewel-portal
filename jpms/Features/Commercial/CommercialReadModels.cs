using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Features.Commercial;

public sealed class ValuationsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Valuation>> valuationsByProject = new();

    public ValuationsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Valuation> Current(string projectId) =>
        valuationsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Valuation>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => valuationsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        valuationsByProject[projectId] = await queries.AskAsync(new ListValuationsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class CostCodeBudgetsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<CostCodeBudget>> budgetsByProject = new();

    public CostCodeBudgetsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<CostCodeBudget> Current(string projectId) =>
        budgetsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<CostCodeBudget>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => budgetsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        budgetsByProject[projectId] = await queries.AskAsync(new ListCostCodeBudgetsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class TimesheetsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Timesheet>> timesheetsByProject = new();

    public TimesheetsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Timesheet> Current(string projectId) =>
        timesheetsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Timesheet>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => timesheetsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        timesheetsByProject[projectId] = await queries.AskAsync(new ListTimesheetsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
