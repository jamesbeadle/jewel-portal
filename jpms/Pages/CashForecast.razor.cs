using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class CashForecast
{
    // Session checked and the user is signed in. This is NOT "the figures are here": the heading,
    // notice, filter and (for directors) the KPI tiles show at once; everything computed waits
    // behind the one gate.
    // The project list is what the filter and every row are built from, and its fetch throws
    // rather than recording per-project failure — so a failure here opens the gate with a message.
    private bool projectsFailed;
    // True once the default (live jobs) selection has been seeded from the loaded project list —
    // before that the table has no honest answer, not even "no projects selected".
    private bool selectionInitialised;

    // How many per-project loads run at once. Each load's reads run sequentially, so this is
    // also the total concurrent-query ceiling (see the Profit Summary page; same reasoning).
    // Each project here costs eleven reads, so the ceiling matters more, not less.
    private const int ProjectRefreshConcurrency = 4;

    // The §4.1 fallback when a project has no contract record: 30 days from issue to cash.
    private const int DefaultPaymentLagDays = 30;

    // The visible month axis is capped so the table stays readable; dated flows beyond it fold
    // into the Later column rather than dropping off.
    private const int MaxVisibleMonths = 12;

    private IReadOnlyCollection<string> selectedIds = Array.Empty<string>();

    // Per-project load state. A project is in exactly one of these once selected: loading (fetch
    // in flight), loaded (every read landed), or failed (any read threw — its row shows zeros
    // behind the banner). Ids, matched case-insensitively like every project id comparison.
    private readonly HashSet<string> loadedProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> failedProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> loadingProjects = new(StringComparer.OrdinalIgnoreCase);

    // Per-project answers to the reads the shared read models don't cache for us.
    private readonly Dictionary<string, ProjectValuationInvoiceSummary> invoiceSummaryByProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<ValuationInvoice>> invoicesByProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProjectRetention?> retentionByProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<WorkOrderInvoiceSummary>> woSummariesByProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<ProjectCostOfSalesLine>> costLinesByProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<PackageReconciliationRow>> packagesByProject = new(StringComparer.OrdinalIgnoreCase);

    // ONE throttle for the page's lifetime, not one per LoadSelectedAsync call — a filter change
    // or retry while the first batch is still in flight must share the same ceiling.
    private readonly SemaphoreSlim throttle = new(ProjectRefreshConcurrency);

    // The interim overheads figure (see the amber notice and ForecastOverheadsStorage), plus
    // per-month overrides (FD, 2026-08-17: "happy with default but can we edit this monthly
    // figure — only when edited it changes"). A month absent from the dictionary follows the
    // default, including a default changed later; only edited months hold their own figure.
    private decimal overheadsMonthly;
    private Dictionary<DateTime, decimal> overheadsOverrides = new();

    private decimal OverheadsFor(DateTime month) =>
        overheadsOverrides.TryGetValue(month, out var overridden) ? overridden : overheadsMonthly;

    // Which forecast category rows are expanded to their per-project lines.
    private readonly HashSet<ForecastCategory> expandedCategories = new();

    // Bank tiles/rows are directors-only, mirroring the API's gate on the Xero cash summary.
    private bool IsDirector =>
        Session.ActiveRole is { } role && DesktopNavigation.CanSee(role, DesktopNavigation.DirectorRoles);

    private XeroCashSummarySnapshot? BankSnapshot => IsDirector ? Cash.Snapshot() : null;

    private bool BankReady => BankSnapshot is { IsConfigured: true, Error: null };

    private string FetchedText =>
        BankSnapshot?.FetchedAtUtc is { } fetched ? fetched.ToLocalTime().ToString("HH:mm") : "—";

    // The selected projects in the canonical work order (re-applied after the filter, per the
    // project-ordering convention) — the rows both sections render.
    private List<Project> SelectedProjects =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .Where(project => selectedIds.Contains(project.ProjectId, StringComparer.OrdinalIgnoreCase))
            .InWorkOrder()
            .ToList();

    private List<Project> FailedSelectedProjects =>
        SelectedProjects.Where(project => failedProjects.Contains(project.ProjectId)).ToList();

    private bool TableReady =>
        selectionInitialised
        && SelectedProjects.All(project =>
            loadedProjects.Contains(project.ProjectId) || failedProjects.Contains(project.ProjectId));

    // ---- Loading ------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Projects.OnChanged += StateHasChanged;
        Summary.OnChanged += StateHasChanged;
        WorkOrders.OnChanged += StateHasChanged;
        Lines.OnChanged += StateHasChanged;
        Claims.OnChanged += StateHasChanged;
        ClaimEntries.OnChanged += StateHasChanged;
        Contracts.OnChange += StateHasChanged;
        Cash.OnChange += StateHasChanged;

        // The bank position loads alongside the table, directors only (stale-while-revalidate;
        // the store's fetch-once guard covers the very first load).
        if (IsDirector) _ = Cash.RefreshAsync();

        if (Auth.CurrentUser is { } user)
        {
            overheadsMonthly = await OverheadsStorage.ReadAsync(user.Email) ?? 0m;
            overheadsOverrides = new Dictionary<DateTime, decimal>(await OverheadsStorage.ReadOverridesAsync(user.Email));
        }

        try
        {
            if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None);
        }
        catch
        {
            // HttpQueryClient has already reported this to the error toast with a reference; here
            // we only need to stop the table waiting on a list that is not coming.
            projectsFailed = true;
            return;
        }
        selectedIds = ProjectMultiSelect.LiveJobIds(Projects.Current ?? Array.Empty<Project>());
        selectionInitialised = true;
        await LoadSelectedAsync();
    }

    private async Task OnSelectionChangedAsync(IReadOnlyCollection<string> ids)
    {
        selectedIds = ids;
        await LoadSelectedAsync();
    }

    private async Task RetryFailedAsync()
    {
        failedProjects.Clear();
        await LoadSelectedAsync();
    }

    // Loads every selected project that hasn't been loaded yet, a few at a time — not all at
    // once. Firing every project's eleven reads together stampedes the serverless database;
    // each project inside the throttle runs its reads sequentially, so the ceiling is
    // ProjectRefreshConcurrency queries in flight. Already-loaded projects are simply re-shown
    // (fetch-once, like the read models beneath) — re-entering the page recreates it
    // (KeyedPageRouteView) and reloads fresh.
    private async Task LoadSelectedAsync()
    {
        var toLoad = SelectedProjects
            .Where(project => !loadedProjects.Contains(project.ProjectId)
                              && !failedProjects.Contains(project.ProjectId)
                              && !loadingProjects.Contains(project.ProjectId))
            .ToList();
        if (toLoad.Count == 0) return;
        foreach (var project in toLoad) loadingProjects.Add(project.ProjectId);
        await Task.WhenAll(toLoad.Select(async project =>
        {
            await throttle.WaitAsync();
            try
            {
                await LoadProjectAsync(project.ProjectId);
                loadedProjects.Add(project.ProjectId);
            }
            catch
            {
                // The read model / query client has already raised the error toast; the row
                // degrades to zeros behind the banner rather than a blank page.
                failedProjects.Add(project.ProjectId);
            }
            finally
            {
                loadingProjects.Remove(project.ProjectId);
                throttle.Release();
            }
        }));
        StateHasChanged();
    }

    private async Task LoadProjectAsync(string projectId)
    {
        // Sequential on purpose — see LoadSelectedAsync. Claims must land before the entries
        // read anyway, because only a Draft latest claim needs its per-line % entries.
        await Summary.RefreshAsync(projectId, CancellationToken.None);
        // The summary read model records failure rather than throwing (its page fires it
        // fire-and-forget); here a silent all-zero row would be a lie, so failure fails the row.
        if (Summary.LastRefreshFailed(projectId))
            throw new InvalidOperationException("The financial summary could not be loaded.");
        await WorkOrders.RefreshAsync(projectId, CancellationToken.None);
        await Lines.RefreshAsync(projectId, CancellationToken.None);
        await Claims.RefreshAsync(projectId, CancellationToken.None);
        invoiceSummaryByProject[projectId] = await Invoices.GetSummaryAsync(projectId);
        // The forecast's two extra reads: the itemised invoices (each phased at its own expected
        // receipt date) and the contract (payment mechanism + completion-date fallback).
        invoicesByProject[projectId] = await Invoices.ListAsync(projectId);
        await Contracts.RefreshAsync(projectId, CancellationToken.None);
        if (LatestClaimFor(projectId) is { Status: ValuationClaimStatus.Draft } draft)
            await ClaimEntries.RefreshAsync(draft.ValuationClaimId, CancellationToken.None);
        retentionByProject[projectId] = await Queries.AskAsync(new GetProjectRetention(projectId), CancellationToken.None);
        woSummariesByProject[projectId] = await Queries.AskAsync(new ListWorkOrderInvoiceSummaries(projectId), CancellationToken.None);
        costLinesByProject[projectId] = await Queries.AskAsync(new ListProjectCostOfSalesLines(projectId), CancellationToken.None);
        packagesByProject[projectId] = await Queries.AskAsync(new ListPackageReconciliation(projectId), CancellationToken.None);
    }

}
