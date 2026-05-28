using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Commercial;

public sealed class ValuationsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Valuation>> valuationsByProject = new();

    public ValuationsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Valuation> Current(string projectId) =>
        valuationsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Valuation>();

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

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        timesheetsByProject[projectId] = await queries.AskAsync(new ListTimesheetsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
