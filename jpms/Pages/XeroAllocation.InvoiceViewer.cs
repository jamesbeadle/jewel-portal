using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // The document itself lives in InvoiceDocumentPreview (fetch, chips, iframe); this page
    // owns which line is open and the allocate-from-the-viewer actions below.

    private XeroLedgerLine? viewLine;
    private string? viewError;

    private void OpenInvoiceView(XeroLedgerLine line)
    {
        viewLine = line;
        viewError = null;
    }

    private void CloseInvoiceView()
    {
        // Closing the viewer abandons a split started inside it — otherwise the
        // standalone split modal (hidden while the viewer is open) would pop up
        // unbidden the moment the viewer goes.
        if (splitLine is not null && splitLine.XeroLedgerLineId == viewLine?.XeroLedgerLineId)
            CloseSplit();
        // Likewise a dispute being composed inside the viewer.
        if (disputeLine is not null && disputeLine.XeroLedgerLineId == viewLine?.XeroLedgerLineId)
            CloseDispute();
        viewLine = null;
        viewError = null;
    }

    /// <summary>
    /// Allocate straight from the document viewer. Success closes the modal
    /// (back to the queue); failure surfaces the reason inside the modal —
    /// the page-level error banner would sit invisibly behind it.
    /// </summary>
    private async Task AllocateFromViewAsync()
    {
        if (viewLine is null || isBusy || !CanAllocate(viewLine)) return;
        var line = viewLine;
        isApplying = true; viewError = null;
        try
        {
            await Ledger.ApplyAsync(new SetXeroAllocation(
                new[] { line.XeroLedgerLineId },
                XeroAllocationAction.Allocate,
                SelectedProjectFor(line),
                SelectedCostCenterFor(line)));
            selectedIds.Remove(line.XeroLedgerLineId);
            chosenProject.Remove(line.XeroLedgerLineId);
            chosenCostCenter.Remove(line.XeroLedgerLineId);
            chosenBucket.Remove(line.XeroLedgerLineId);
            if (viewLine == line) CloseInvoiceView();
        }
        catch (CommandFailedException failure)
        {
            if (viewLine == line) viewError = failure.Message;
        }
        finally { isApplying = false; }
    }

    /// <summary>
    /// The Set half-step from the document viewer: the reader identified the
    /// site but not yet the cost centre. Saves the project (the line stays
    /// queued under its project tab, Xero site written best-effort) and closes.
    /// </summary>
    private async Task SetProjectFromViewAsync()
    {
        if (viewLine is null || isBusy || !CanSetProject(viewLine)) return;
        var line = viewLine;
        isApplying = true; viewError = null;
        try
        {
            await Ledger.ApplyAsync(new SetXeroAllocation(
                new[] { line.XeroLedgerLineId },
                XeroAllocationAction.SetProject,
                SelectedProjectFor(line)));
            chosenProject.Remove(line.XeroLedgerLineId);
            if (viewLine == line) CloseInvoiceView();
        }
        catch (CommandFailedException failure)
        {
            if (viewLine == line) viewError = failure.Message;
        }
        finally { isApplying = false; }
    }

    private async Task IgnoreFromViewAsync() =>
        await ApplyFromViewAsync(line => new SetXeroAllocation(
            new[] { line.XeroLedgerLineId },
            XeroAllocationAction.Ignore));

    private async Task BucketFromViewAsync()
    {
        if (viewLine is null || !CanBucket(viewLine)) return;
        await ApplyFromViewAsync(line => new SetXeroAllocation(
            new[] { line.XeroLedgerLineId },
            XeroAllocationAction.AllocateToBucket,
            Bucket: SelectedBucketFor(line)));
    }

    /// <summary>
    /// The viewer's counterpart of ApplyAsync for the one-shot decisions (Ignore,
    /// bucket): the line leaves the queue, so its picks are forgotten and the
    /// viewer closes; a failure stays readable inside the modal.
    /// </summary>
    private async Task ApplyFromViewAsync(Func<XeroLedgerLine, SetXeroAllocation> buildCommand)
    {
        if (viewLine is null || isBusy) return;
        var line = viewLine;
        isApplying = true; viewError = null;
        try
        {
            await Ledger.ApplyAsync(buildCommand(line));
            ForgetPicks(line.XeroLedgerLineId);
            if (viewLine == line) CloseInvoiceView();
        }
        catch (CommandFailedException failure)
        {
            if (viewLine == line) viewError = failure.Message;
        }
        finally { isApplying = false; }
    }

    private void ForgetPicks(string lineId)
    {
        selectedIds.Remove(lineId);
        chosenProject.Remove(lineId);
        chosenCostCenter.Remove(lineId);
        chosenBucket.Remove(lineId);
    }

}
