using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
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
}
