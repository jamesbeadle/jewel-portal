using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;


namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // Mirrors the server's rule: persisted coding (Set, or the agreement carried
    // out of a resolved dispute) counts as matched — a human decision beats the
    // suggestion, never the other way. Labour-recognised lines are excluded by the
    // SERVER's rule (matched worker or covered, regardless of this visit's
    // "not labour" picks), because the one-shot allocation skips them server-side:
    // settlement bills leave the queue through covering and the §6a coding run.
    private int FullyMatchedCount =>
        UnallocatedLines.Count(line => line.MatchedWorkerId is null && !line.CoveredByTimesheets
                                       && (line.ProjectId ?? line.SuggestedProjectId) is not null
                                       && (line.CostCenterCode ?? line.SuggestedCostCenterCode) is not null);

    private async Task AllocateMatchedAsync()
    {
        isApplying = true; errorMessage = null; confirmAllocateMatched = false;
        try
        {
            var allocated = await Ledger.AllocateSuggestedAsync();
            syncMessage = $"{allocated} matched lines allocated — what's left in the queue needs a human decision.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private async Task ApplyAsync(SetXeroAllocation command)
    {
        isApplying = true; errorMessage = null;
        try
        {
            await Ledger.ApplyAsync(command);
            foreach (var id in command.XeroLedgerLineIds)
            {
                selectedIds.Remove(id);
                chosenProject.Remove(id);
                chosenCostCenter.Remove(id);
                chosenBucket.Remove(id);
            }
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    // -- Labour section actions ------------------------------------------------
    // Marking a line as settlement is the §6 cover, worker-month scoped: the approved timesheet
    // is the actual, the bill is settlement of it. The re-read of the unallocated queue is what
    // moves the line into the covered fold — the covered flag rides the ledger line.

    private static DateTimeOffset? SettlementMonthOf(XeroLedgerLine line) =>
        line.Date is { } date
            // date.Year/Month only — never the DateTime itself, whose Kind would make the
            // offset constructor throw for Local kinds (the BST lesson).
            ? new DateTimeOffset(new DateTime(date.Year, date.Month, 1), TimeSpan.Zero)
            : null;

    private static bool CanMarkCover(XeroLedgerLine line) =>
        line.MatchedSubcontractorId is not null && line.Date is not null;

    // ---- Inline settlement-identity fix on the Labour tab (2026-08-31) -------------------------

    private readonly Dictionary<string, string> inlineLinkPicks = new();

    private IReadOnlyList<SearchSelect.Option> DirectoryOptions =>
        Subcontractors.All().Where(s => !s.IsProspect)
            .OrderBy(s => s.CompanyName)
            .Select(s => new SearchSelect.Option(s.SubcontractorId, s.CompanyName))
            .ToList();

    private string InlineLinkPickFor(XeroLedgerLine line) =>
        inlineLinkPicks.TryGetValue(line.XeroLedgerLineId, out var pick) ? pick : "";

    private async Task LinkWorkerInlineAsync(XeroLedgerLine line)
    {
        if (isBusy || line.MatchedWorkerId is null) return;
        var pick = InlineLinkPickFor(line);
        if (pick == "") return;
        isApplying = true; errorMessage = null;
        try
        {
            await Labour.SetWorkerSettlementIdentityAsync(line.MatchedWorkerId, pick, isSoleTrader: false);
            inlineLinkPicks.Remove(line.XeroLedgerLineId);
            // Recognition re-runs on the reload, so the row's Mark-as-settlement arms at once.
            await Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
            syncMessage = $"{line.MatchedWorkerName} linked — settlement now reconciles through the directory company.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private async Task MarkSoleTraderInlineAsync(XeroLedgerLine line)
    {
        if (isBusy || line.MatchedWorkerId is null) return;
        isApplying = true; errorMessage = null;
        try
        {
            await Labour.SetWorkerSettlementIdentityAsync(line.MatchedWorkerId, subcontractorId: null, isSoleTrader: true);
            await Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
            syncMessage = $"{line.MatchedWorkerName} flagged sole trader — they settle under their own name.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private string MarkCoverHint(XeroLedgerLine line) =>
        line.MatchedSubcontractorId is null
            ? "Covering reconciles by settlement counterparty — link a company or flag the worker a sole trader (inline, left) first"
            : line.Date is null
                ? "The line has no bill date, so there is no month to settle against"
                : $"Mark this line as settlement of {line.MatchedWorkerName}'s {SettlementMonthOf(line):MMMM yyyy} timesheets — covered value is excluded from cost-of-sales aggregations; the worker-month verdict lives on the Labour overview's Settlement view";

    private async Task MarkCoverAsync(XeroLedgerLine line)
    {
        if (isBusy || !CanMarkCover(line)) return;
        var month = SettlementMonthOf(line)!.Value;
        isApplying = true; errorMessage = null;
        try
        {
            await Labour.SetTimesheetCoverForMonthAsync(line.XeroLedgerLineId, true, line.MatchedSubcontractorId!, month);
            await Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
            selectedIds.Remove(line.XeroLedgerLineId);
            syncMessage = $"{line.ContactName ?? line.MatchedWorkerName} · {Money(SignedNet(line))} marked as settlement of {month:MMMM yyyy} — reconciled on the Labour overview's Settlement view.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private async Task UnmarkCoverAsync(XeroLedgerLine line)
    {
        if (isBusy) return;
        isApplying = true; errorMessage = null;
        try
        {
            await Labour.SetTimesheetCoverForMonthAsync(
                line.XeroLedgerLineId, false, line.MatchedSubcontractorId ?? "",
                line.CoveredPeriodStart ?? SettlementMonthOf(line) ?? DateTimeOffset.UtcNow);
            await Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
            syncMessage = $"{line.ContactName ?? "The line"} · {Money(SignedNet(line))} un-marked — back in the outstanding labour list.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private IReadOnlyList<XeroLedgerLine> SelectedLabour =>
        Lines is null
            ? Array.Empty<XeroLedgerLine>()
            : Lines.Where(line => line.AllocationStatus == XeroAllocationStatus.Unallocated
                                  && IsLabourLine(line)
                                  && selectedIds.Contains(line.XeroLedgerLineId)).ToList();

    private async Task BulkMarkCoverAsync()
    {
        if (isBusy) return;
        var outstanding = SelectedLabour.Where(line => !line.CoveredByTimesheets).ToList();
        var markable = outstanding.Where(CanMarkCover).ToList();
        var skipped = outstanding.Count - markable.Count;
        if (markable.Count == 0 && skipped == 0) return;

        isApplying = true; errorMessage = null;
        try
        {
            foreach (var line in markable)
            {
                await Labour.SetTimesheetCoverForMonthAsync(
                    line.XeroLedgerLineId, true, line.MatchedSubcontractorId!, SettlementMonthOf(line)!.Value);
                // Removed per line, so a failure part-way leaves exactly the unmarked lines
                // selected for a retry.
                selectedIds.Remove(line.XeroLedgerLineId);
            }
            await Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
            syncMessage = $"{markable.Count} {(markable.Count == 1 ? "line" : "lines")} marked as settlement."
                + (skipped > 0
                    ? $" {skipped} skipped — no linked subcontractor company or no bill date (fix on the Workers page, then Re-check matches)."
                    : "");
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private async Task BulkUnmarkCoverAsync()
    {
        if (isBusy) return;
        var covered = SelectedLabour.Where(line => line.CoveredByTimesheets).ToList();
        if (covered.Count == 0) return;

        isApplying = true; errorMessage = null;
        try
        {
            foreach (var line in covered)
            {
                await Labour.SetTimesheetCoverForMonthAsync(
                    line.XeroLedgerLineId, false, line.MatchedSubcontractorId ?? "",
                    line.CoveredPeriodStart ?? DateTimeOffset.UtcNow);
                selectedIds.Remove(line.XeroLedgerLineId);
            }
            await Ledger.RefreshAsync(XeroAllocationStatus.Unallocated);
            syncMessage = $"{covered.Count} {(covered.Count == 1 ? "line" : "lines")} un-marked — back in the outstanding labour list.";
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

    private IReadOnlyList<DropdownMenu.Item> LabourRowMenuItems(XeroLedgerLine line)
    {
        var items = new List<DropdownMenu.Item>
        {
            new("Not labour — allocate normally",
                OnSelect: EventCallback.Factory.Create(this, () => TreatAsOrdinaryCost(line)),
                Hint: "A false match: sends the line back to the ordinary queue for this visit. Fix the real cause — the worker's name or subcontractor link — on the Workers page")
        };
        items.Add(new DropdownMenu.Item("Ignore",
            OnSelect: EventCallback.Factory.Create(this, () => IgnoreAsync(line)),
            Hint: "Not a project cost — moves the line to the Ignored tab",
            Group: DecisionMenuGroup));
        items.Add(new DropdownMenu.Item("Dispute…",
            OnSelect: EventCallback.Factory.Create(this, () => OpenDispute(line)),
            Hint: "Contest this cost — moves it to the Disputed tab and opens a discussion with the accountant",
            Group: DecisionMenuGroup));
        return items;
    }

    private void TreatAsOrdinaryCost(XeroLedgerLine line)
    {
        notLabourIds.Add(line.XeroLedgerLineId);
        selectedIds.Remove(line.XeroLedgerLineId);
        syncMessage = $"{line.ContactName ?? "The line"} treated as an ordinary cost for this visit — it is back in the queue. Recognition recomputes from the worker registry on every read.";
    }

}
