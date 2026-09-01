using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // The list is fetched live from Xero when the modal opens; the bytes are
    // proxied through the API on demand — nothing is stored in JPMS.

    private XeroLedgerLine? viewLine;
    private IReadOnlyList<XeroInvoiceAttachment>? viewAttachments;
    private XeroInvoiceAttachment? viewSelected;
    private string? viewError;

    private async Task OpenInvoiceViewAsync(XeroLedgerLine line)
    {
        viewLine = line;
        viewAttachments = null;
        viewSelected = null;
        viewError = null;
        try
        {
            var attachments = await Queries.AskAsync(
                new ListXeroInvoiceAttachments(line.XeroInvoiceId, line.Type == "ACCPAYCREDIT"),
                CancellationToken.None);
            if (viewLine != line) return; // The modal was closed (or reopened) while fetching.
            viewAttachments = attachments;
            // PDFs and images preview in place; pre-select the first previewable one.
            viewSelected = attachments.FirstOrDefault(IsPreviewable) ?? attachments.FirstOrDefault();
            if (attachments.Count == 0)
                viewError = "Xero holds no documents for this invoice — it may have been removed since the last sync.";
        }
        catch (Exception)
        {
            if (viewLine != line) return;
            viewError = "Couldn't fetch the invoice's documents from Xero. If this keeps happening, check that the "
                + "Xero custom connection has the accounting.attachments scope ticked in the Xero developer portal.";
        }
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
        viewAttachments = null;
        viewSelected = null;
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

    // Same previewable set as the triage and drawing viewers: browsers render PDFs
    // and images natively in an iframe; everything else gets a Download link only.
    private static bool IsPreviewable(XeroInvoiceAttachment attachment) =>
        attachment.MimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || attachment.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    // The file name travels in the query string, never the path — supplier file
    // names carry spaces and characters that don't survive a URL path segment.
    private string InvoiceAttachmentUrl(XeroInvoiceAttachment attachment, bool inline) =>
        $"/api/xero/invoice/attachment?id={Uri.EscapeDataString(viewLine?.XeroInvoiceId ?? "")}"
        + $"&file={Uri.EscapeDataString(attachment.FileName)}"
        + (viewLine?.Type == "ACCPAYCREDIT" ? "&credit=1" : "")
        + (inline ? "&inline=1" : "");

    private string InvoiceAttachmentChipClass(XeroInvoiceAttachment attachment) =>
        (viewSelected == attachment
            ? "bg-content text-surface border-content"
            : "bg-surface text-content-muted border-line hover:text-content")
        + " text-xs font-medium border rounded-full px-3 py-1 transition-colors";
}
