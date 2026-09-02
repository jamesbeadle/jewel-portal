using Jewel.JPMS.Commercial;
using static Jewel.JPMS.Features.Commercial.ValuationReportDisplay;

namespace Jewel.JPMS.Components;

public partial class ValuationReportTable
{
    // ---- Bulk % entry -------------------------------------------------------
    // Every counting line's % becomes an input at once; a single Save sends only the
    // changed lines as one RecordClaimEntries command. Sections auto-expand so the whole
    // bill can be tabbed through top to bottom.
    private bool bulkEditing;
    private bool bulkSaving;
    private string? bulkError;
    private readonly Dictionary<string, string> bulkValues = new();
    private readonly HashSet<string> bulkErrorLines = new();

    private string? bulkClaimId;

    private void StartBulkEdit()
    {
        CancelEdit(); // close any single-cell editor first
        bulkEditing = true;
        bulkClaimId = SelectedClaim?.ValuationClaimId;
        bulkError = null;
        bulkErrorLines.Clear();
        bulkValues.Clear();
        foreach (var line in lines.Where(l => l.CountsTowardTotals))
            bulkValues[line.ValuationLineItemId] = PercentText(PercentFor(line));
        foreach (ValuationElementType type in Enum.GetValues<ValuationElementType>())
            expanded.Add(type);
        OpenEveryRollUp(); // every line's % is typed individually, so the consolidated rows must show them
    }

    private void CancelBulkEdit()
    {
        bulkEditing = false;
        bulkError = null;
        bulkErrorLines.Clear();
        bulkValues.Clear();
    }

    private string BulkValueFor(ValuationLineItem line) =>
        bulkValues.TryGetValue(line.ValuationLineItemId, out var value) ? value : "";

    private void SetBulkValue(ValuationLineItem line, string value)
    {
        bulkValues[line.ValuationLineItemId] = value;
        bulkErrorLines.Remove(line.ValuationLineItemId);
    }

    private int BulkChangedCount => lines.Count(l =>
        l.CountsTowardTotals
        && bulkValues.TryGetValue(l.ValuationLineItemId, out var raw)
        && TryParsePercent(raw, out var percent)
        && percent != PercentFor(l));

    private static bool TryParsePercent(string raw, out decimal percent) =>
        decimal.TryParse((raw ?? "").Trim().TrimEnd('%'), System.Globalization.NumberStyles.Number, Gb, out percent);

    // Any number, any number of decimal places, positive or negative, on every kind of line.
    // The % is whatever reproduces the claimed value (% x line amount), and that is legitimately
    // negative or >100 on a VO whose omits are claimed ahead of its additions, and needs more
    // than two decimal places wherever a claimed amount has been worked back to a % of a large
    // line — clipping the decimals is exactly what put the pennies out on the valuation report.
    // +/-100000 is just a sanity rail against typos (the API applies the same one).
    private const decimal PercentRail = 100000m;

    private static bool WithinBounds(decimal percent) =>
        percent >= -PercentRail && percent <= PercentRail;

    private async Task SaveBulkAsync()
    {
        if (SelectedClaim is null || bulkSaving) return;
        bulkError = null;
        bulkErrorLines.Clear();

        var entries = new List<ClaimEntryInput>();
        foreach (var line in lines.Where(l => l.CountsTowardTotals))
        {
            if (!bulkValues.TryGetValue(line.ValuationLineItemId, out var raw)) continue;
            if (!TryParsePercent(raw, out var percent) || !WithinBounds(percent))
            {
                bulkErrorLines.Add(line.ValuationLineItemId);
                continue;
            }
            if (percent != PercentFor(line))
                entries.Add(new ClaimEntryInput(line.ValuationLineItemId, percent));
        }
        if (bulkErrorLines.Count > 0)
        {
            bulkError = $"{bulkErrorLines.Count} value(s) aren't valid percentages — fix the highlighted cells (a number between -100000 and 100000).";
            return;
        }
        if (entries.Count == 0) { CancelBulkEdit(); return; }

        bulkSaving = true;
        try
        {
            await Store.RecordEntriesAsync(ProjectId, new RecordClaimEntries(SelectedClaim.ValuationClaimId, entries));
            CancelBulkEdit();
            OnParametersSet();
        }
        catch (CommandFailedException failure)
        {
            bulkError = failure.Message;
        }
        catch
        {
            bulkError = "Couldn't save the percentages. Please try again.";
        }
        finally { bulkSaving = false; }
    }

}
