using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // Session checked and the user is signed in. This is NOT "the figures are here": the heading
    // and filter show at once; the tiles, bridge and table wait behind their own signals.
    // The project list is what the filter and every row are built from, and its fetch throws
    // rather than recording per-project failure — so a failure here opens the gate with a message.
    private bool projectsFailed;
    // True once the default (live jobs) selection has been seeded from the loaded project list —
    // before that the table has no honest answer, not even "no projects selected".
    private bool selectionInitialised;

    // How many per-project loads run at once — see the Cash Summary page; same reasoning.
    private const int ProjectRefreshConcurrency = 4;

    private IReadOnlyCollection<string> selectedIds = Array.Empty<string>();

    // Per-project load state. A project is in exactly one of these once selected: loading (fetch
    // in flight), loaded (every read landed), or failed (any read threw — its row shows zeros
    // behind the banner). Ids, matched case-insensitively like every project id comparison.
    private readonly HashSet<string> loadedProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> failedProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> loadingProjects = new(StringComparer.OrdinalIgnoreCase);

    // Per-project answers to the reads the shared read models don't cache for us.
    // Certified gross + the deposit credits inside it (see ProjectValuationInvoiceSummary).
    private readonly Dictionary<string, (decimal Certified, decimal DepositCredited)> invoicedByProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<PackageReconciliationRow>> packagesByProject = new(StringComparer.OrdinalIgnoreCase);

    // ONE throttle for the page's lifetime, not one per LoadSelectedAsync call — a filter change
    // or retry while the first batch is still in flight must share the same ceiling.
    private readonly SemaphoreSlim throttle = new(ProjectRefreshConcurrency);

    // The selected projects in the canonical work order (re-applied after the filter, per the
    // project-ordering convention) — the rows the table renders.
    private List<Project> SelectedProjects =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .Where(project => selectedIds.Contains(project.ProjectId, StringComparer.OrdinalIgnoreCase))
            .InWorkOrder()
            .ToList();

    private List<Project> FailedSelectedProjects =>
        SelectedProjects.Where(project => failedProjects.Contains(project.ProjectId)).ToList();

    // The Xero panels' row order: the SAME order the table is showing — league order by
    // default, the clicked column when sorted — so a reader can run a finger across the
    // page (Jeremy paired Coombe Lane's row with By France's when the orders differed,
    // 2026-08-13). Until the table's figures have landed the work order stands in; the
    // hand-over doesn't reshuffle mid-read because the whole table region is gated on
    // TableReady anyway.
    private List<Project> GridProjects => TableReady
        ? LoadedRows().Select(entry => entry.Project).ToList()
        : SelectedProjects;

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
        SitePnl.OnChanged += StateHasChanged;
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
        // The table's per-project loads and the Xero panel's single read are independent
        // regions with independent gates — load them in parallel.
        await Task.WhenAll(LoadSelectedAsync(), LoadSitePnlAsync());
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
    // once (see the Cash Summary page; same reasoning and same ceiling).
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
        var invoiceSummary = await Invoices.GetSummaryAsync(projectId);
        invoicedByProject[projectId] = (invoiceSummary.TotalCertified, invoiceSummary.TotalDepositCredited);
        if (LatestClaimFor(projectId) is { Status: ValuationClaimStatus.Draft } draft)
            await ClaimEntries.RefreshAsync(draft.ValuationClaimId, CancellationToken.None);
        packagesByProject[projectId] = await Queries.AskAsync(new ListPackageReconciliation(projectId), CancellationToken.None);
    }

    public void Dispose()
    {
        Projects.OnChanged -= StateHasChanged;
        Summary.OnChanged -= StateHasChanged;
        WorkOrders.OnChanged -= StateHasChanged;
        Lines.OnChanged -= StateHasChanged;
        Claims.OnChanged -= StateHasChanged;
        ClaimEntries.OnChanged -= StateHasChanged;
        SitePnl.OnChanged -= StateHasChanged;
        // The throttle is deliberately NOT disposed: a load still in flight when the user
        // navigates away would Release() a disposed semaphore and fault the abandoned task.
        // An undisposed SemaphoreSlim (no wait-handle use) holds nothing worth reclaiming.
    }

}
