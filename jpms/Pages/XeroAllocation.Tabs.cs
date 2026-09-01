using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // -- project tabs -----------------------------------------------------------
    // One tab per project that has incoming lines pointing at it — persisted (Set)
    // or Xero-suggested — alphabetical. Dropdown picks don't count: a row moves
    // tabs only when Set or Allocate makes the project official. The open tab
    // stays in the bar even at zero lines — allocating the last line mustn't yank
    // the user elsewhere, and a remembered tab restored on load may be empty.

    private IReadOnlyList<(string ProjectId, string Name, int Count)>? projectTabsCache;
    private (IReadOnlyList<XeroLedgerLine>? Lines, object Projects, string? ProjectTab, int NotLabour) projectTabsCacheKey;

    private IReadOnlyList<(string ProjectId, string Name, int Count)> ProjectTabs
    {
        get
        {
            var key = ((IReadOnlyList<XeroLedgerLine>?)UnallocatedLines, (object)Projects, activeProjectId, notLabourIds.Count);
            if (projectTabsCache is null || key != projectTabsCacheKey)
            {
                // Labour-recognised lines belong to the Labour section, never to a project tab —
                // even when their Xero tracking suggests a site.
                var tabs = UnallocatedLines
                    .Where(line => !IsLabourLine(line))
                    .GroupBy(GroupProjectFor)
                    .Where(group => group.Key != "")
                    .Select(group => (ProjectId: group.Key, Name: ProjectName(group.Key), Count: group.Count()))
                    .ToList();
                if (activeTab == XeroAllocationStatus.Unallocated && activeProjectId is not null
                    && !tabs.Any(tab => tab.ProjectId == activeProjectId))
                    tabs.Add((activeProjectId, ProjectName(activeProjectId), 0));
                projectTabsCache = tabs.OrderBy(tab => tab.Name, StringComparer.OrdinalIgnoreCase).ToList();
                projectTabsCacheKey = key;
            }
            return projectTabsCache;
        }
    }

    // The tab a line belongs to: its persisted project (Set) or, failing that,
    // the Xero suggestion. Deliberately blind to chosenProject — see ProjectTabs.
    private string GroupProjectFor(XeroLedgerLine line)
    {
        var value = line.ProjectId ?? line.SuggestedProjectId ?? "";
        return ValidProjectIds.Contains(value) ? value : "";
    }

    private int UnassignedCount =>
        UnallocatedLines.Count(line => !IsLabourLine(line) && GroupProjectFor(line) == "");

    // -- Labour section (scope §6 recognition) ----------------------------------
    // Recognition itself is server-side, on the line (matched worker + covered flag, computed
    // next to the tracking suggester); the page only partitions on it. notLabourIds is the
    // this-visit escape hatch back to the ordinary queue.

    private bool IsLabourLine(XeroLedgerLine line) =>
        (line.MatchedWorkerId is not null || line.CoveredByTimesheets)
        && !notLabourIds.Contains(line.XeroLedgerLineId);

    /// <summary>The actionable number the tab shows: recognised lines not yet marked.</summary>
    private int LabourOutstandingCount =>
        UnallocatedLines.Count(line => IsLabourLine(line) && !line.CoveredByTimesheets);

    private int LabourCoveredCount =>
        UnallocatedLines.Count(line => IsLabourLine(line) && line.CoveredByTimesheets);

    private int LabourTotalCount => LabourOutstandingCount + LabourCoveredCount;

    // Every stored line, from the server's counts — what the page-level "has anything been synced
    // yet?" gate asks about. Deliberately not derived from the loaded tab: the tab bar is inside
    // that gate, and a remembered-but-empty tab would otherwise lock the user out of the page.
    private static int LedgerTotal(XeroLedgerCounts counts) =>
        counts.Unallocated + counts.Allocated + counts.Bucketed + counts.Ignored + counts.Disputed;

    // Whether the empty tab is empty because of a filter the user set, or just empty.
    private bool HasTabFilters =>
        !string.IsNullOrWhiteSpace(search) || bucketFilter is not null
        || allocatedProjectFilter != "" || activeProjectId is not null || labourTab;

    private void SwitchTab(XeroAllocationStatus tab, string? projectId = null, bool labour = false)
    {
        activeTab = tab;
        labourTab = tab == XeroAllocationStatus.Unallocated && labour;
        activeProjectId = tab == XeroAllocationStatus.Unallocated && !labour ? projectId : null;
        showCoveredLabour = false;
        selectedIds.Clear();
        bucketFilter = null;
        allocatedProjectFilter = "";
        CloseSendToCostCentre();
        CloseSendToProject();
        CloseDispute();
        CloseDiscussion();
        openRowMenuKey = null;
        page = 0;
        // On a project tab every line is already pointed at that project, so
        // pre-arm the bulk bar's project with it.
        if (activeProjectId is not null) bulkProjectId = activeProjectId;
        // Reading Lines starts the fetch for a status not yet held; this just makes it explicit
        // (and revalidates a tab already in hand, per the stale-while-revalidate convention).
        _ = Ledger.RefreshAsync(tab);
        _ = PersistTabAsync();
    }

    // -- last-tab memory --------------------------------------------------------
    // The screen reopens on whatever tab the user last had open (per browser, per
    // user): project people land straight back on their project's queue.

    private const string ProjectTabPrefix = "Project:";
    private const string LabourTabToken = "Labour";

    private async Task RestoreLastTabAsync()
    {
        var stored = await TabStorage.ReadAsync(Auth.CurrentUser!.Email);
        if (string.IsNullOrWhiteSpace(stored)) return;
        if (stored.StartsWith(ProjectTabPrefix, StringComparison.Ordinal))
        {
            activeTab = XeroAllocationStatus.Unallocated;
            activeProjectId = stored[ProjectTabPrefix.Length..];
            bulkProjectId = activeProjectId;
        }
        else if (stored == LabourTabToken)
        {
            activeTab = XeroAllocationStatus.Unallocated;
            labourTab = true;
        }
        else if (Enum.TryParse<XeroAllocationStatus>(stored, out var status))
        {
            activeTab = status;
        }
    }

    private Task PersistTabAsync() =>
        Auth.CurrentUser is null
            ? Task.CompletedTask
            : TabStorage.WriteAsync(Auth.CurrentUser.Email,
                labourTab ? LabourTabToken
                : activeProjectId is not null ? $"{ProjectTabPrefix}{activeProjectId}" : activeTab.ToString());

    // -- display helpers ------------------------------------------------------

    // From the server's GROUP BY, not from counting a downloaded list: the tab bar shows a number
    // for every status while the page only ever holds one status' lines.
    private int CountOf(XeroAllocationStatus status) =>
        Ledger.Counts()?.For(status) ?? 0;

    // The plain Unallocated tab is active only when neither a project tab nor Labour is open.
    private string TabClass(XeroAllocationStatus tab) =>
        TabCss(activeTab == tab && (tab != XeroAllocationStatus.Unallocated || (activeProjectId is null && !labourTab)));

    private string ProjectTabClass(string projectId) =>
        TabCss(activeTab == XeroAllocationStatus.Unallocated && activeProjectId == projectId);

    private string LabourTabClass() =>
        TabCss(activeTab == XeroAllocationStatus.Unallocated && labourTab);

    private static string TabCss(bool active) =>
        (active ? "bg-content text-surface" : "bg-surface text-content-muted hover:text-content")
        + " text-sm font-medium px-4 py-1.5 transition-colors";
}
