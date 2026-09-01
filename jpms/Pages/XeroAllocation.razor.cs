using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // Session checked and the user is signed in. This is NOT "the ledger is here" — the heading,
    // the Sync and Re-check buttons show at once; the tab bar and the rows wait behind their own
    // gates.
    private bool sessionReady;

    // Which tab is open decides which status is fetched and which counts the bar shows, so the
    // ledger section waits for the remembered choice rather than opening on the wrong tab.
    private bool tabRestored;

    private bool isSyncing;
    private bool isRechecking;
    private bool isApplying;
    private string search = string.Empty;
    private string? syncMessage;
    private string? errorMessage;
    private XeroAllocationStatus activeTab = XeroAllocationStatus.Unallocated;

    // Which project tab of the incoming queue is open. Only meaningful while
    // activeTab is Unallocated: null is the plain "Unallocated" tab (no project
    // set yet); otherwise the tab for that project. Grouping follows only what's
    // real — the persisted project (Set) or the Xero suggestion — never the
    // dropdown pick, so choosing a project doesn't move the row anywhere until
    // Set or Allocate makes it official.
    private string? activeProjectId;

    private readonly HashSet<string> selectedIds = new();
    private readonly Dictionary<string, string?> chosenProject = new();
    private readonly Dictionary<string, string?> chosenCostCenter = new();
    private readonly Dictionary<string, string?> chosenBucket = new();
    private string bulkProjectId = "";
    private string bulkCostCenterCode = "";
    private string bulkBucket = "";
    private string? bucketFilter;
    private bool confirmAllocateMatched;

    // -- Labour section state (scope §6 recognition) ---------------------------
    // labourTab is a sub-view of the Unallocated status, like activeProjectId: the tab the
    // recognised settlement bills fall in instead of the queue. Covered lines fold away once
    // marked (showCoveredLabour reveals them); notLabourIds is this visit's escape hatch for a
    // false match — the line rejoins the plain queue for a normal allocation. Recognition is
    // recomputed server-side on every ledger read, so the set is not persisted: a mis-match is
    // fixed properly on the Workers page (rename/deactivate), not remembered here.
    private bool labourTab;
    private bool showCoveredLabour;
    private readonly HashSet<string> notLabourIds = new();

    // -- Allocated-tab recode state --------------------------------------------
    // "" = all projects. The filter narrows the Allocated tab to one project so
    // its coding can be reviewed, selected and re-sent in bulk.
    private string allocatedProjectFilter = "";
    private bool sendToCostCentreOpen;
    private string sendCostCenterCode = "";
    private string? sendError;
    private bool sendToProjectOpen;
    private string sendProjectId = "";
    private string? sendProjectError;

    // -- Dispute state ---------------------------------------------------------
    // disputeLine: the line the "Dispute…" modal is composing an opening message
    // for (a snapshot is fine — nothing about it changes while composing).
    // discussLineId: the DISPUTED line whose thread modal is open — held as an id
    // and re-resolved from the store each render, so a sent reply (which reloads
    // the tab) shows up in the open modal instead of a stale snapshot.
    private XeroLedgerLine? disputeLine;
    private string disputeMessage = "";
    private string? disputeError;
    private string? discussLineId;
    private string discussMessage = "";
    private string? discussError;

    private XeroLedgerLine? DiscussLine =>
        discussLineId is null
            ? null
            : Ledger.Lines(XeroAllocationStatus.Disputed)?.FirstOrDefault(line => line.XeroLedgerLineId == discussLineId);

    private bool isBusy => isSyncing || isRechecking || isApplying;

    // -- Panel gates. Each lists every source the panel reads, so it appears in one piece. --
    // Both panels name their rows through the project list and the cost-centre master, and both
    // offer them as dropdown options — neither can be drawn before those land.
    private bool MastersReady => ProjectsReadModel.Current is not null && CostCenters.IsLoaded;

    private bool TabBarReady => Ledger.Lines(XeroAllocationStatus.Unallocated) is not null && MastersReady;

    private bool QueueReady => Lines is not null && MastersReady;

    // The lines for the tab currently open. Reading this starts that status' load if it hasn't
    // happened yet, so a tab switch fetches just that tab.
    private IReadOnlyList<XeroLedgerLine>? Lines => Ledger.Lines(activeTab);

    // The unallocated queue, which the tab bar itself is built from (its project tabs and their
    // counts) and which the "allocate all matched" banner counts against — so it is loaded whatever
    // tab is open. On the Unallocated tab this is the same list as Lines.
    private IReadOnlyList<XeroLedgerLine> UnallocatedLines =>
        Ledger.Lines(XeroAllocationStatus.Unallocated) ?? Array.Empty<XeroLedgerLine>();

    private IReadOnlyList<Jewel.JPMS.Models.Project> Projects =>
        ProjectsReadModel.Current ?? Array.Empty<Jewel.JPMS.Models.Project>();

    // -- filtering & paging ----------------------------------------------------
    // The filtered list is memoized: Blazor reads Visible several times per render
    // and with thousands of lines the repeated LINQ passes (and re-rendering every
    // row's dropdowns) is what made tab switches crawl. Only PageSize rows render.

    private const int PageSize = 50;
    private int page;
    private IReadOnlyList<XeroLedgerLine>? visibleCache;
    private (IReadOnlyList<XeroLedgerLine>? Lines, object Projects, string Search, XeroAllocationStatus Tab,
             string? ProjectTab, string? Bucket, string AllocatedProject,
             bool LabourTab, bool ShowCovered, int NotLabour) visibleCacheKey;

    private IReadOnlyList<XeroLedgerLine> Visible
    {
        get
        {
            // Projects is in the key because GroupProjectFor validates against it.
            var key = (Lines, (object)Projects, search, activeTab, activeProjectId, bucketFilter, allocatedProjectFilter,
                       labourTab, showCoveredLabour, notLabourIds.Count);
            if (visibleCache is null || key != visibleCacheKey)
            {
                visibleCache = Lines is null
                    ? Array.Empty<XeroLedgerLine>()
                    // Filtering by status is the server's job now — Lines IS the open tab's set —
                    // but the check stays as a cheap guard against rendering the previous tab's
                    // rows in the moment between a switch and its fetch landing.
                    : Lines.Where(line => line.AllocationStatus == activeTab)
                           // The queue and the Labour section partition the unallocated set: a
                           // recognised line renders only in Labour, everything else only in the
                           // plain/project queue — nothing appears twice, nothing disappears.
                           .Where(line => activeTab != XeroAllocationStatus.Unallocated
                                          || (labourTab
                                              ? IsLabourLine(line) && (showCoveredLabour || !line.CoveredByTimesheets)
                                              : !IsLabourLine(line) && GroupProjectFor(line) == (activeProjectId ?? "")))
                           .Where(line => activeTab != XeroAllocationStatus.Bucketed
                                          || bucketFilter is null || line.Bucket == bucketFilter)
                           .Where(line => activeTab != XeroAllocationStatus.Allocated
                                          || allocatedProjectFilter == ""
                                          || MatchesAllocatedProjectFilter(line))
                           .Where(MatchesSearch)
                           .ToList();
                visibleCacheKey = key;
                page = Math.Clamp(page, 0, Math.Max(0, (visibleCache.Count - 1) / PageSize));
            }
            return visibleCache;
        }
    }

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

    private int PageCount => Math.Max(1, (Visible.Count + PageSize - 1) / PageSize);

    private IEnumerable<XeroLedgerLine> Paged =>
        Visible.Skip(page * PageSize).Take(PageSize);

    private void OnSearchChanged(ChangeEventArgs args)
    {
        search = args.Value?.ToString() ?? string.Empty;
        page = 0;
    }

    private async Task ResolveDisputeAsync(XeroLedgerLine line)
    {
        if (isBusy) return;
        // Unsaved dropdown picks travel with the resolution: they are saved first,
        // then the line resolves — so "pick, then Return to allocation" needs no
        // separate Save. If the save fails, the resolve does not run.
        var projectId = SelectedProjectFor(line);
        var centre = SelectedCostCenterFor(line);
        isApplying = true; errorMessage = null;
        try
        {
            if (!string.IsNullOrEmpty(projectId)
                && (projectId != line.ProjectId || (string.IsNullOrEmpty(centre) ? null : centre) != line.CostCenterCode))
                await Ledger.ApplyAsync(new SetXeroAllocation(
                    new[] { line.XeroLedgerLineId },
                    XeroAllocationAction.SetProject,
                    projectId,
                    string.IsNullOrEmpty(centre) ? null : centre));
            await Ledger.ApplyAsync(new SetXeroAllocation(
                new[] { line.XeroLedgerLineId }, XeroAllocationAction.ResolveDispute));
            selectedIds.Remove(line.XeroLedgerLineId);
            chosenProject.Remove(line.XeroLedgerLineId);
            chosenCostCenter.Remove(line.XeroLedgerLineId);
            chosenBucket.Remove(line.XeroLedgerLineId);
            if (discussLineId == line.XeroLedgerLineId) CloseDiscussion();
            syncMessage = "Dispute resolved — the line is back in the allocation queue"
                + (string.IsNullOrEmpty(projectId) ? "." : $" under {ProjectName(projectId)}, ready to allocate.");
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private async Task SendToProjectAsync()
    {
        if (isBusy || string.IsNullOrEmpty(sendProjectId)) return;
        var sendable = ProjectSendableSelected;
        var skipped = SelectedAllocated.Count - sendable.Count;
        if (sendable.Count == 0) return;

        isApplying = true; sendProjectError = null;
        try
        {
            foreach (var group in sendable.GroupBy(line => line.CostCenterCode!))
            {
                var ids = group.Select(line => line.XeroLedgerLineId).ToList();
                await Ledger.ApplyAsync(new SetXeroAllocation(
                    ids, XeroAllocationAction.Allocate, sendProjectId, group.Key));
                // Cleared per group, not at the end: a failure part-way leaves
                // exactly the unapplied lines selected for a retry.
                foreach (var id in ids)
                {
                    selectedIds.Remove(id);
                    chosenProject.Remove(id);
                    chosenCostCenter.Remove(id);
                    chosenBucket.Remove(id);
                }
            }
            sendToProjectOpen = false;
            syncMessage = $"{sendable.Count} {(sendable.Count == 1 ? "line" : "lines")} sent to {ProjectName(sendProjectId)}."
                + (skipped > 0
                    ? $" {skipped} selected lines are split across cost centres and were left unchanged — undo and re-split those individually."
                    : "");
        }
        catch (CommandFailedException failure) { sendProjectError = failure.Message; }
        finally { isApplying = false; }
    }

    // The line-level project catches whole-line allocations and single-project
    // splits (the API keeps the common project on the line); per-split projects
    // catch a multi-project split any time one share belongs to the filter.
    private bool MatchesAllocatedProjectFilter(XeroLedgerLine line) =>
        line.ProjectId == allocatedProjectFilter
        || (line.Splits?.Any(split => (split.ProjectId ?? line.ProjectId) == allocatedProjectFilter) ?? false);

    private void OnAllocatedProjectFilterChanged(string? projectId)
    {
        allocatedProjectFilter = projectId ?? "";
        page = 0;
    }

    // The filter takes ProjectOptions as-is: SearchSelect already leads its unfiltered list with a
    // blank entry labelled with the Placeholder ("All projects"), so adding one here printed it
    // twice.

    private async Task BucketAsync(XeroLedgerLine line, string bucket) =>
        await ApplyAsync(new SetXeroAllocation(new[] { line.XeroLedgerLineId }, XeroAllocationAction.AllocateToBucket, Bucket: bucket));

    // -- queue row menu -----------------------------------------------------------
    // Everything a queue row can do beyond Allocate and Set. A bucket entry
    // allocates in one click (there is no arming step — the row's primary button
    // already covers a suggested bucket); the suggested one is ticked.

    private const int BucketMenuGroup = 1;
    private const int DecisionMenuGroup = 2;
    private const int UndoMenuGroup = 3;

    private IReadOnlyList<DropdownMenu.Item> QueueRowMenuItems(XeroLedgerLine line)
    {
        var items = new List<DropdownMenu.Item>
        {
            new("Split…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenSplit(line)),
                Hint: "Share this line's value across several cost centres (or projects) — one Xero line per share on approval")
        };
        items.AddRange(BucketMenuItems(line));
        items.Add(new DropdownMenu.Item("Ignore",
            OnSelect: EventCallback.Factory.Create(this, () => IgnoreAsync(line)),
            Hint: "Not a project cost — moves the line to the Ignored tab",
            Group: DecisionMenuGroup));
        items.Add(new DropdownMenu.Item("Dispute…",
            OnSelect: EventCallback.Factory.Create(this, () => OpenDispute(line)),
            Hint: "Contest this cost — moves it to the Disputed tab and opens a discussion with the accountant",
            Group: DecisionMenuGroup));
        if (line.ProjectId is not null)
        {
            items.Add(new DropdownMenu.Item("Unset project",
                OnSelect: EventCallback.Factory.Create(this, () => UnsetProjectAsync(line)),
                Hint: "Clear the saved project (and its Xero site) — the line returns to Unallocated",
                Group: UndoMenuGroup));
        }
        return items;
    }

    private IEnumerable<DropdownMenu.Item> BucketMenuItems(XeroLedgerLine line)
    {
        var armed = SelectedBucketFor(line);
        foreach (var bucket in XeroBuckets.All)
        {
            var chosen = bucket;
            yield return new DropdownMenu.Item($"Allocate to {chosen}",
                OnSelect: EventCallback.Factory.Create(this, () => BucketAsync(line, chosen)),
                Hint: $"Allocate to the {chosen} bucket — no project, no cost centre",
                Selected: chosen == armed,
                Group: BucketMenuGroup);
        }
        if (!string.IsNullOrEmpty(armed))
        {
            yield return new DropdownMenu.Item("Clear bucket",
                OnSelect: EventCallback.Factory.Create(this, () => SetBucket(line.XeroLedgerLineId, null)),
                Hint: "Disarm the bucket so the row allocates to its project and cost centre instead",
                Group: BucketMenuGroup);
        }
    }

    // The queue row whose menu is open — while set, the table container's
    // overflow cap is lifted so the menu isn't clipped (see the container).
    private string? openRowMenuKey;

    private void TrackRowMenu(string lineId, bool isOpen)
    {
        if (isOpen) { openRowMenuKey = lineId; return; }
        if (openRowMenuKey == lineId) openRowMenuKey = null;
    }

    // Only a menu on a row that is still on the page keeps the cap lifted — a
    // line that left the page mid-menu (allocated elsewhere, filtered out) must
    // not leave every later table uncapped.
    private bool IsRowMenuOpen =>
        openRowMenuKey is not null && Paged.Any(line => line.XeroLedgerLineId == openRowMenuKey);

    // Date, invoice number, Xero site and Xero code on one small line under the
    // description — what used to be the Date column and the description's footnote.
    private string LineMetaText(XeroLedgerLine line) =>
        $"{DateText(line.Date)} · {line.InvoiceNumber ?? "—"} · {line.XeroSite ?? "no site"} · {line.XeroCostCode ?? "no Xero code"}";

    // The bucket dropdown works like the project/cost-centre ones: pre-selected with
    // the suggestion when one was inferred, blank otherwise, always overridable.
    private string SelectedBucketFor(XeroLedgerLine line) =>
        (chosenBucket.TryGetValue(line.XeroLedgerLineId, out var chosen) ? chosen : line.SuggestedBucket) ?? "";

    private void SetBucket(string lineId, string? bucket) => chosenBucket[lineId] = bucket;

    private bool CanBucket(XeroLedgerLine line) => !string.IsNullOrEmpty(SelectedBucketFor(line));

    private async Task BulkBucketAsync() =>
        await ApplyAsync(new SetXeroAllocation(selectedIds.ToList(), XeroAllocationAction.AllocateToBucket, Bucket: bulkBucket));

    private decimal BucketTotal(string bucket) =>
        Lines?.Where(line => line.AllocationStatus == XeroAllocationStatus.Bucketed && line.Bucket == bucket)
              .Sum(SignedNet) ?? 0m;

    private void ToggleBucketFilter(string bucket) =>
        bucketFilter = bucketFilter == bucket ? null : bucket;

    private string BucketChipClass(string bucket) =>
        (bucketFilter == bucket
            ? "bg-content text-surface border-content"
            : "bg-surface text-content-muted border-line hover:text-content")
        + " text-xs font-medium border rounded-full px-3 py-1 transition-colors";

    // -- selection & per-line choices ----------------------------------------

    // A value that isn't in the dropdown's options (e.g. a suggestion for a project or
    // code that no longer exists) counts as empty — otherwise the select renders blank
    // while the Allocate button believes a value is set.
    // Priority: the user's local pick > the persisted project (a Set line stays
    // queued with its project saved server-side) > the Xero-tracking suggestion.
    private string SelectedProjectFor(XeroLedgerLine line)
    {
        var value = (chosenProject.TryGetValue(line.XeroLedgerLineId, out var chosen)
            ? chosen
            : line.ProjectId ?? line.SuggestedProjectId) ?? "";
        return ValidProjectIds.Contains(value) ? value : "";
    }

    // GroupProjectFor runs for every line when grouping the queue into project
    // tabs (not just the 50 rendered rows), so the validity check is a set
    // lookup rather than a scan of the project list per line.
    private HashSet<string>? validProjectIdsCache;
    private object? validProjectIdsCacheKey;

    private HashSet<string> ValidProjectIds
    {
        get
        {
            if (validProjectIdsCache is null || !ReferenceEquals(validProjectIdsCacheKey, Projects))
            {
                validProjectIdsCache = Projects.Select(project => project.ProjectId).ToHashSet();
                validProjectIdsCacheKey = Projects;
            }
            return validProjectIdsCache;
        }
    }

    // Priority mirrors SelectedProjectFor: the user's local pick > the persisted
    // centre (saved mid-dispute, or carried out of a resolved dispute) > the
    // Xero-tracking suggestion.
    private string SelectedCostCenterFor(XeroLedgerLine line)
    {
        var value = (chosenCostCenter.TryGetValue(line.XeroLedgerLineId, out var chosen)
            ? chosen
            : line.CostCenterCode ?? line.SuggestedCostCenterCode) ?? "";
        return CostCenters.Active().Any(centre => centre.Code == value) ? value : "";
    }

    // A dropdown pick arms the row's buttons but moves nothing — the row changes
    // tab only when Set or Allocate persists the project.
    // A project or cost-centre pick is the later, deliberate act, so it disarms a
    // bucket (suggested or chosen) — otherwise the row's primary button would
    // still read "Allocate to Fuel" after the reader coded the line to a project.
    private void SetProject(string lineId, string? projectId)
    {
        chosenProject[lineId] = projectId;
        chosenBucket[lineId] = null;
    }

    private void SetCostCenter(string lineId, string? code)
    {
        chosenCostCenter[lineId] = code;
        chosenBucket[lineId] = null;
    }

    private bool CanAllocate(XeroLedgerLine line) =>
        !string.IsNullOrEmpty(SelectedProjectFor(line)) && !string.IsNullOrEmpty(SelectedCostCenterFor(line));

    // -- searchable dropdown options -------------------------------------------
    // Option lists for the SearchSelect dropdowns, memoized on the source list
    // references (both stores hand back stable lists until they refresh) so the
    // 50 rendered rows don't rebuild them per component per render.

    private IReadOnlyList<SearchSelect.Option>? projectOptionsCache;
    private object? projectOptionsCacheKey;

    // Order comes from the read model, which serves the canonical work order (live sites first,
    // Completed last) — the list is not re-sorted here. Completed projects stay in it: old costs
    // still get recoded onto them.
    private IReadOnlyList<SearchSelect.Option> ProjectOptions
    {
        get
        {
            if (projectOptionsCache is null || !ReferenceEquals(projectOptionsCacheKey, Projects))
            {
                projectOptionsCache = Projects
                    .Select(project => new SearchSelect.Option(project.ProjectId, project.Name)).ToList();
                projectOptionsCacheKey = Projects;
            }
            return projectOptionsCache;
        }
    }

    private IReadOnlyList<SearchSelect.Option>? costCenterOptionsCache;
    private object? costCenterOptionsCacheKey;

    // The label carries code + name so typing matches either ("kit" finds
    // CARP-KIT Carpentry kitchens; "ELE" finds every electrical centre).
    private IReadOnlyList<SearchSelect.Option> CostCenterOptions
    {
        get
        {
            var centres = CostCenters.ActiveAlphabetical();
            if (costCenterOptionsCache is null || !ReferenceEquals(costCenterOptionsCacheKey, centres))
            {
                costCenterOptionsCache = centres
                    .Select(centre => new SearchSelect.Option(centre.Code, $"{centre.Code} {centre.Name}")).ToList();
                costCenterOptionsCacheKey = centres;
            }
            return costCenterOptionsCache;
        }
    }

    private void ToggleSelected(string lineId)
    {
        if (!selectedIds.Remove(lineId)) selectedIds.Add(lineId);
    }

    // Select-all works on the current page — a deliberate guard against silently
    // bulk-acting on thousands of filtered lines. Narrow with search, then select.
    private bool AllVisibleSelected
    {
        get
        {
            var any = false;
            foreach (var line in Paged) { any = true; if (!selectedIds.Contains(line.XeroLedgerLineId)) return false; }
            return any;
        }
    }

    private void ToggleSelectAll(ChangeEventArgs args)
    {
        if (args.Value is true)
            foreach (var line in Paged) selectedIds.Add(line.XeroLedgerLineId);
        else
            selectedIds.Clear();
    }

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

    private bool MatchesSearch(XeroLedgerLine line)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var term = search.Trim();
        return Contains(line.ContactName) || Contains(line.Description) || Contains(line.InvoiceNumber)
            || Contains(line.Reference) || Contains(line.XeroSite) || Contains(line.XeroCostCode)
            // Allocated lines also match on the centre(s) they were coded to —
            // code or name (CostCenterText carries both) — so a mis-coded run
            // can be found by where it landed, then re-sent in bulk.
            || (line.CostCenterCode is not null && Contains(CostCenterText(line.CostCenterCode)))
            || (line.Splits?.Any(split => Contains(CostCenterText(split.CostCenterCode))) ?? false);

        bool Contains(string? value) =>
            value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private string ProjectName(string? projectId) =>
        Projects.FirstOrDefault(project => project.ProjectId == projectId)?.Name ?? projectId ?? "—";

    private string CostCenterText(string? code)
    {
        if (code is null) return "—";
        var centre = CostCenters.All().FirstOrDefault(c => c.Code == code);
        return centre is null ? code : $"{centre.Code} {centre.Name}";
    }

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

    private static decimal SignedNet(XeroLedgerLine line) =>
        line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;

    private static string DateText(DateTime? date) => date?.ToString("d MMM yyyy") ?? "—";

    private static string Money(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

    public void Dispose()
    {
        Ledger.OnChange -= StateHasChanged;
        CostCenters.OnChange -= StateHasChanged;
        ProjectsReadModel.OnChanged -= StateHasChanged;
        Subcontractors.OnChange -= StateHasChanged;
    }
}
