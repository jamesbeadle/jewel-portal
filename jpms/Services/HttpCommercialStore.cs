using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCommercialStore : ICommercialStore
{
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyList<Valuation>> valuationsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<CostCodeBudget>> budgetsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<Timesheet>> timesheetsByProject = new();

    public HttpCommercialStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<ClaimPeriod> ClaimPeriodsFor(string projectId) => Array.Empty<ClaimPeriod>();

    public IReadOnlyList<Valuation> ValuationsFor(string projectId)
    {
        if (!valuationsByProject.ContainsKey(projectId)) _ = LoadValuationsAsync(projectId);
        return valuationsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Valuation>();
    }

    public Valuation SaveValuation(Valuation valuation)
    {
        _ = PostAndRefreshAsync("/api/valuations", valuation, valuation.ProjectId, LoadValuationsAsync);
        return valuation;
    }

    public IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId)
    {
        if (!budgetsByProject.ContainsKey(projectId)) _ = LoadBudgetsAsync(projectId);
        return budgetsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<CostCodeBudget>();
    }

    public IReadOnlyList<Timesheet> TimesheetsFor(string projectId)
    {
        if (!timesheetsByProject.ContainsKey(projectId)) _ = LoadTimesheetsAsync(projectId);
        return timesheetsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Timesheet>();
    }

    public Timesheet SaveTimesheet(Timesheet timesheet)
    {
        _ = PostAndRefreshAsync("/api/timesheets", timesheet, timesheet.ProjectId, LoadTimesheetsAsync);
        return timesheet;
    }

    public Timesheet ApproveTimesheet(string timesheetId)
    {
        var existing = timesheetsByProject.Values.SelectMany(list => list).First(t => t.TimesheetId == timesheetId);
        return SaveTimesheet(existing with { IsApproved = true });
    }

    public CashflowSnapshot LatestCashflow() =>
        new("CF-001", DateTimeOffset.UtcNow, 1_180_000m, 920_000m, 260_000m);

    private async Task LoadValuationsAsync(string projectId)
    {
        try { valuationsByProject[projectId] = (await httpClient.GetFromJsonAsync<List<Valuation>>($"/api/projects/{projectId}/valuations"))?.AsReadOnly() ?? (IReadOnlyList<Valuation>)Array.Empty<Valuation>(); OnChange?.Invoke(); }
        catch { valuationsByProject[projectId] = Array.Empty<Valuation>(); }
    }

    private async Task LoadBudgetsAsync(string projectId)
    {
        try { budgetsByProject[projectId] = (await httpClient.GetFromJsonAsync<List<CostCodeBudget>>($"/api/projects/{projectId}/cost-code-budgets"))?.AsReadOnly() ?? (IReadOnlyList<CostCodeBudget>)Array.Empty<CostCodeBudget>(); OnChange?.Invoke(); }
        catch { budgetsByProject[projectId] = Array.Empty<CostCodeBudget>(); }
    }

    private async Task LoadTimesheetsAsync(string projectId)
    {
        try { timesheetsByProject[projectId] = (await httpClient.GetFromJsonAsync<List<Timesheet>>($"/api/projects/{projectId}/timesheets"))?.AsReadOnly() ?? (IReadOnlyList<Timesheet>)Array.Empty<Timesheet>(); OnChange?.Invoke(); }
        catch { timesheetsByProject[projectId] = Array.Empty<Timesheet>(); }
    }

    private async Task PostAndRefreshAsync<T>(string url, T body, string projectId, Func<string, Task> refresh)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        await refresh(projectId);
    }
}
