using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Commercial;

public sealed class ValuationLinesReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ValuationLineItem>> linesByProject = new();

    public ValuationLinesReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ValuationLineItem> Current(string projectId) =>
        linesByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ValuationLineItem>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        linesByProject[projectId] = await queries.AskAsync(new ListValuationLinesForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class ValuationClaimsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ValuationClaim>> claimsByProject = new();

    public ValuationClaimsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ValuationClaim> Current(string projectId) =>
        claimsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ValuationClaim>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        claimsByProject[projectId] = await queries.AskAsync(new ListValuationClaimsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class ClaimLinesReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ClaimLine>> linesByClaim = new();

    public ClaimLinesReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ClaimLine> Current(string claimId) =>
        linesByClaim.TryGetValue(claimId, out var list) ? list : Array.Empty<ClaimLine>();

    public async Task RefreshAsync(string claimId, CancellationToken cancellationToken)
    {
        linesByClaim[claimId] = await queries.AskAsync(new ListClaimLines(claimId), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>
/// Snapshot headers (newest first) for one project's Valuation Report tab — the immutable
/// frozen copies behind invoice submissions and on-demand period-end records. Fetch-once
/// per project; ProjectValuation.razor calls RefreshAsync via the store's Refresh
/// (stale-while-revalidate). Lines are fetched per snapshot on demand, not cached here.
/// </summary>
public sealed class ValuationReportSnapshotsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ValuationReportSnapshot>> snapshotsByProject = new();

    public ValuationReportSnapshotsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ValuationReportSnapshot> Current(string projectId) =>
        snapshotsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ValuationReportSnapshot>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        snapshotsByProject[projectId] = await queries.AskAsync(new ListValuationReportSnapshotsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>
/// Named cost-centre roll-ups for one project's Financials tab. Fetch-once per
/// project; ProjectFinancials.razor calls RefreshAsync(projectId) from
/// OnInitializedAsync (stale-while-revalidate).
/// </summary>
public sealed class CostCentreGroupsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<CostCentreGroup>> groupsByProject = new();

    public CostCentreGroupsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<CostCentreGroup> Current(string projectId) =>
        groupsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<CostCentreGroup>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        groupsByProject[projectId] = await queries.AskAsync(new ListCostCentreGroupsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>
/// Per-project financial summary (budget from the valuation report, actuals from
/// allocated Xero lines). Fetch-once per project; ProjectFinancials.razor calls
/// RefreshAsync(projectId) from OnInitializedAsync (stale-while-revalidate).
/// </summary>
public sealed class ProjectFinancialSummaryReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ProjectFinancialSummaryRow>> rowsByProject = new();
    private readonly HashSet<string> failedProjects = new();

    public ProjectFinancialSummaryReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ProjectFinancialSummaryRow> Current(string projectId) =>
        rowsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProjectFinancialSummaryRow>();

    /// <summary>True when the last refresh for this project failed. The Financials page fires
    /// RefreshAsync fire-and-forget, so without this a failed fetch silently renders as
    /// all-zero rows — the page uses this flag to show an error banner instead.</summary>
    public bool LastRefreshFailed(string projectId) => failedProjects.Contains(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        try
        {
            rowsByProject[projectId] = await queries.AskAsync(new GetProjectFinancialSummary(projectId), cancellationToken);
            failedProjects.Remove(projectId);
        }
        catch
        {
            failedProjects.Add(projectId);
        }
        OnChanged?.Invoke();
    }
}
