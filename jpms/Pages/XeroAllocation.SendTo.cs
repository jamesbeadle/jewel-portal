using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;


namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // previously split line moves whole; the rare split spanning MULTIPLE
    // projects has no line-level project to keep and is left unchanged (called
    // out in the modal) rather than silently guessed at.

    private IReadOnlyList<XeroLedgerLine> SelectedAllocated =>
        selectedIds.Count == 0
            ? Array.Empty<XeroLedgerLine>()
            : Lines?.Where(line => line.AllocationStatus == XeroAllocationStatus.Allocated
                                   && selectedIds.Contains(line.XeroLedgerLineId)).ToList()
              ?? (IReadOnlyList<XeroLedgerLine>)Array.Empty<XeroLedgerLine>();

    // A multi-project split's ProjectId is null (see XeroLedgerLine.Splits).
    private IReadOnlyList<XeroLedgerLine> SendableSelected =>
        SelectedAllocated.Where(line => line.ProjectId is not null).ToList();

    private void OpenSendToCostCentre()
    {
        sendToCostCentreOpen = true;
        sendCostCenterCode = "";
        sendError = null;
    }

    private void CloseSendToCostCentre()
    {
        sendToCostCentreOpen = false;
        sendError = null;
    }

    private async Task SendToCostCentreAsync()
    {
        if (isBusy || string.IsNullOrEmpty(sendCostCenterCode)) return;
        var sendable = SendableSelected;
        var skipped = SelectedAllocated.Count - sendable.Count;
        if (sendable.Count == 0) return;

        isApplying = true; sendError = null;
        try
        {
            foreach (var group in sendable.GroupBy(line => line.ProjectId!))
            {
                var ids = group.Select(line => line.XeroLedgerLineId).ToList();
                await Ledger.ApplyAsync(new SetXeroAllocation(
                    ids, XeroAllocationAction.Allocate, group.Key, sendCostCenterCode));
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
            sendToCostCentreOpen = false;
            syncMessage = $"{sendable.Count} {(sendable.Count == 1 ? "line" : "lines")} sent to {CostCenterText(sendCostCenterCode)}."
                + (skipped > 0
                    ? $" {skipped} selected lines span multiple projects and were left unchanged — undo and re-split those individually."
                    : "");
        }
        catch (CommandFailedException failure) { sendError = failure.Message; }
        finally { isApplying = false; }
    }

    private async Task BulkUndoAsync() =>
        await ApplyAsync(new SetXeroAllocation(selectedIds.ToList(), XeroAllocationAction.Reset));

    // -- Allocated-tab bulk re-site --------------------------------------------
    // The change-of-mind move: costs allocated to (or coded by the accountant
    // onto) the wrong project get sent to the right one without undoing. The
    // mirror of "Send to cost centre": each line keeps its centre, only the
    // project changes — sent as one Allocate per cost-centre group, since the
    // API allocates a batch to a single project + centre. Re-allocating
    // replaces any stored split, so a line split across centres has no single
    // centre to keep and is left unchanged (called out in the modal).

    // A split line's CostCenterCode is null (its centres live in Splits rows).
    private IReadOnlyList<XeroLedgerLine> ProjectSendableSelected =>
        SelectedAllocated.Where(line => line.CostCenterCode is not null).ToList();

    private void OpenSendToProject()
    {
        sendToProjectOpen = true;
        sendProjectId = "";
        sendProjectError = null;
    }

    private void CloseSendToProject()
    {
        sendToProjectOpen = false;
        sendProjectError = null;
    }

    // -- Disputes ---------------------------------------------------------------
    // The lifecycle: Dispute… (queue or Allocated tab) parks the line in the
    // Disputed bucket with an optional opening message — the tab count is what
    // tells the accountant, exactly like Unallocated does. On the Disputed tab
    // the two sides talk in the line's thread, Save the coding they agree on
    // (visible to each other, line stays disputed), and either one presses
    // Return to allocation: back to the queue with the agreed coding kept, so a
    // fully coded line lands on its project's tab armed for Allocate. Xero's
    // Sites tracking is written at resolution, not while the dispute is open.

    private void OpenDispute(XeroLedgerLine line)
    {
        disputeLine = line;
        disputeMessage = "";
        disputeError = null;
    }

    private void CloseDispute()
    {
        disputeLine = null;
        disputeError = null;
    }

    private async Task ConfirmDisputeAsync()
    {
        if (disputeLine is null || isBusy) return;
        var line = disputeLine;
        isApplying = true; disputeError = null;
        try
        {
            await Ledger.ApplyAsync(new SetXeroAllocation(
                new[] { line.XeroLedgerLineId },
                XeroAllocationAction.Dispute,
                Note: string.IsNullOrWhiteSpace(disputeMessage) ? null : disputeMessage.Trim()));
            selectedIds.Remove(line.XeroLedgerLineId);
            CloseDispute();
            // A dispute composed inside the document viewer closes it too — same
            // contract as Allocate and Split from the viewer: back to the queue.
            if (viewLine?.XeroLedgerLineId == line.XeroLedgerLineId) CloseInvoiceView();
            syncMessage = $"{line.ContactName ?? "The line"} · {Money(SignedNet(line))} moved to Disputed — the discussion lives on that tab.";
        }
        catch (CommandFailedException failure) { disputeError = failure.Message; }
        finally { isApplying = false; }
    }

    private void OpenDiscussion(XeroLedgerLine line)
    {
        discussLineId = line.XeroLedgerLineId;
        discussMessage = "";
        discussError = null;
    }

    private void CloseDiscussion()
    {
        discussLineId = null;
        discussError = null;
    }

    private async Task SendDiscussionMessageAsync()
    {
        if (discussLineId is null || isBusy || string.IsNullOrWhiteSpace(discussMessage)) return;
        isApplying = true; discussError = null;
        try
        {
            await Ledger.ApplyAsync(new SetXeroAllocation(
                new[] { discussLineId },
                XeroAllocationAction.AddDisputeMessage,
                Note: discussMessage.Trim()));
            discussMessage = "";
            // The store reload has already refreshed the Disputed tab, so DiscussLine
            // now resolves to the line carrying the new message.
        }
        catch (CommandFailedException failure) { discussError = failure.Message; }
        finally { isApplying = false; }
    }

    /// <summary>
    /// Persists the coding being converged on mid-dispute (project required,
    /// centre optional) — the line stays disputed; the other side sees the
    /// proposal. Xero is not touched until the dispute is resolved.
    /// </summary>
    private async Task SaveDisputedCodingAsync(XeroLedgerLine line)
    {
        var projectId = SelectedProjectFor(line);
        if (string.IsNullOrEmpty(projectId)) return;
        var centre = SelectedCostCenterFor(line);
        await ApplyAsync(new SetXeroAllocation(
            new[] { line.XeroLedgerLineId },
            XeroAllocationAction.SetProject,
            projectId,
            string.IsNullOrEmpty(centre) ? null : centre));
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
}
