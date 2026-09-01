using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;

using static Jewel.JPMS.MoneyFormats;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{
    // -- split editor ----------------------------------------------------------
    // A line's value shared across several projects and/or cost centres. The
    // amounts must add up to the line's net exactly; the API enforces the same
    // rules server-side. The editor renders in two places over the same state:
    // the standalone split modal (from a queue row's Split…) and inline in the
    // invoice document modal (from its Split…), where the accountant can read
    // the invoice while keying the shares.


    private sealed class SplitRow
    {
        public string ProjectId { get; set; } = "";
        public string Code { get; set; } = "";
        public decimal? Amount { get; set; }
    }

    private XeroLedgerLine? splitLine;
    private string? splitError;
    private List<SplitRow> splitRows = new();

    private void OpenSplit(XeroLedgerLine line)
    {
        splitLine = line;
        splitError = null;
        // First row pre-filled from the line's chosen/suggested project + centre;
        // second row starts on the same project (same-project splits are the
        // common case) with the centre left open.
        var project = SelectedProjectFor(line);
        splitRows = new List<SplitRow>
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

    // New rows inherit the previous row's project — retyping the same project
    // for every row of a same-project split would be pure friction.
    private void AddSplitRow() =>
        splitRows.Add(new SplitRow { ProjectId = splitRows.LastOrDefault()?.ProjectId ?? "" });

    private void RemoveSplitRow(SplitRow row)
    {
        if (splitRows.Count > 2) splitRows.Remove(row);
    }

    private void OnSplitProjectChanged(SplitRow row, string? projectId) => row.ProjectId = projectId ?? "";

    private void OnSplitCodeChanged(SplitRow row, string? code) => row.Code = code ?? "";

    private void OnSplitAmountChanged(SplitRow row, string? raw)
    {
        row.Amount = decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
        AutoBalance(row);
    }

    // Keying an amount drops the outstanding remainder into another row, so a
    // two-way split needs only one number typed. The target is the first row
    // BELOW the edited one whose amount is still empty or zero (wrapping to
    // earlier rows if none below qualifies). A row already holding a non-zero
    // amount is never overwritten — if the user has keyed every other row,
    // nothing moves and the tally line reports any mismatch instead, keeping
    // the allocation right without clobbering deliberate figures.
    private void AutoBalance(SplitRow edited)
    {
        if (splitLine is null || edited.Amount is null) return;
        var index = splitRows.IndexOf(edited);
        if (index < 0) return;

        var target = splitRows.Skip(index + 1).FirstOrDefault(row => (row.Amount ?? 0m) == 0m)
                     ?? splitRows.Take(index).FirstOrDefault(row => (row.Amount ?? 0m) == 0m);
        if (target is null) return;

        var remaining = splitLine.Net - splitRows.Where(row => row != target).Sum(row => row.Amount ?? 0m);
        if (remaining > 0m) target.Amount = remaining;
    }

    private decimal SplitAssigned => splitRows.Sum(row => row.Amount ?? 0m);
    private decimal SplitRemaining => (splitLine?.Net ?? 0m) - SplitAssigned;

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
        { splitError = $"The amounts must add up to {Money(splitLine.Net)} net — currently {Money(SplitAssigned)}."; return; }

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
