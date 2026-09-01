using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // -- split editor ----------------------------------------------------------
    // A line's value shared across several projects and/or cost centres. The
    // amounts must add up to the line's net exactly; the API enforces the same
    // rules server-side. SplitEditorForm renders the rows in two places over the
    // same draft list — the standalone split modal (from a queue row's Split…)
    // and inline in the invoice document modal (from its Split…), where the
    // accountant can read the invoice while keying the shares. The page owns
    // the drafts so a split started in one place carries on in the other.

    private XeroLedgerLine? splitLine;
    private string? splitError;
    private List<XeroSplitDraft> splitRows = new();

    private void OpenSplit(XeroLedgerLine line)
    {
        splitLine = line;
        splitError = null;
        // First row pre-filled from the line's chosen/suggested project + centre;
        // second row starts on the same project (same-project splits are the
        // common case) with the centre left open.
        var project = SelectedProjectFor(line);
        splitRows = new List<XeroSplitDraft>
        {
            new() { ProjectId = project, Code = SelectedCostCenterFor(line) },
            new() { ProjectId = project }
        };
    }

    private void CloseSplit()
    {
        splitLine = null;
        splitError = null;
    }

    private async Task ApplySplitAsync()
    {
        if (splitLine is null || isBusy) return;

        var rows = splitRows
            .Where(row => !string.IsNullOrEmpty(row.ProjectId) || !string.IsNullOrEmpty(row.Code) || row.Amount is not null)
            .ToList();
        if (rows.Count < 2) { splitError = "A split needs at least two rows — use Allocate for a single project + centre."; return; }
        if (rows.Any(row => string.IsNullOrEmpty(row.ProjectId) || string.IsNullOrEmpty(row.Code) || row.Amount is not > 0m))
        { splitError = "Every row needs a project, a cost centre and an amount above zero."; return; }
        if (rows.Select(row => $"{row.ProjectId}:{row.Code}").Distinct(StringComparer.OrdinalIgnoreCase).Count() != rows.Count)
        { splitError = "Each project + cost centre combination can appear only once."; return; }
        if (rows.Sum(row => row.Amount!.Value) != splitLine.Net)
        { splitError = $"The amounts must add up to {Money(splitLine.Net)} net — currently {Money(splitRows.Sum(row => row.Amount ?? 0m))}."; return; }

        isApplying = true; splitError = null;
        try
        {
            var lineId = splitLine.XeroLedgerLineId;
            await Ledger.ApplyAsync(new SetXeroAllocation(
                new[] { lineId },
                XeroAllocationAction.Allocate,
                Splits: rows.Select(row => new XeroCostSplit(row.Code, row.Amount!.Value, row.ProjectId)).ToList()));
            selectedIds.Remove(lineId);
            chosenProject.Remove(lineId);
            chosenCostCenter.Remove(lineId);
            chosenBucket.Remove(lineId);
            CloseSplit();
            // A split keyed inside the document viewer closes it too on success —
            // same contract as Allocate from the viewer: back to the queue.
            if (viewLine?.XeroLedgerLineId == lineId) CloseInvoiceView();
        }
        catch (CommandFailedException failure) { splitError = failure.Message; }
        finally { isApplying = false; }
    }

    // -- invoice document viewer -------------------------------------------------
    // The supplier's document as Xero holds it (published by Dext with the bill).
}
