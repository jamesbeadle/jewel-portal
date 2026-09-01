using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Components;

public partial class ValuationReportTable
{
    // ---- Consolidated % edit ---------------------------------------------------
    // Shares the single-cell editor's state (editValue / editError / editSaving / editInput) so
    // exactly one editor is ever open. Enter or the tick saves, Esc or the cross discards, and
    // clicking away saves a changed, valid value — the same stance as a line's % editor.
    private void StartRollUpEdit(VariationRollUp<ValuationLineItem> rollUp)
    {
        CancelEdit();
        CancelVoRevise();
        editingRollUpKey = rollUp.Key;
        editValue = RollUpPercent(rollUp).ToString("0.##", Gb);
        editError = false;
        focusPending = true;
    }

    private void CancelRollUpEdit()
    {
        editingRollUpKey = null;
        editError = false;
    }

    private async Task OnRollUpEditKeyDownAsync(KeyboardEventArgs e, VariationRollUp<ValuationLineItem> rollUp)
    {
        if (e.Key == "Enter") await CommitRollUpEditAsync(rollUp);
        else if (e.Key == "Escape") CancelRollUpEdit();
    }

    private async Task OnRollUpEditBlurAsync(VariationRollUp<ValuationLineItem> rollUp)
    {
        if (editSaving || editingRollUpKey != rollUp.Key) return;
        await CommitRollUpEditAsync(rollUp);
    }

    private bool EveryLineAlreadyAt(VariationRollUp<ValuationLineItem> rollUp, decimal percent) =>
        rollUp.CountingLines.All(line => PercentFor(line) == percent);

    private async Task CommitRollUpEditAsync(VariationRollUp<ValuationLineItem> rollUp)
    {
        if (editSaving) return;
        if (SelectedClaim is null) { CancelRollUpEdit(); return; }
        if (!TryParsePercent(editValue, out var percent) || !WithinBounds(rollUp.Lines[0], percent))
        {
            editError = true;
            return;
        }
        // Unchanged: either every line already sits at this % or the editor's opening (weighted)
        // figure was left as it was — a click-away must never flatten a per-line split.
        if (EveryLineAlreadyAt(rollUp, percent) || percent == RollUpPercent(rollUp)) { CancelRollUpEdit(); return; }

        editSaving = true;
        try
        {
            var entries = rollUp.CountingLines
                .Select(line => new ClaimEntryInput(line.ValuationLineItemId, percent))
                .ToList();
            await Store.RecordEntriesAsync(ProjectId, new RecordClaimEntries(SelectedClaim.ValuationClaimId, entries));
            if (editingRollUpKey == rollUp.Key) CancelRollUpEdit();
            OnParametersSet();
        }
        catch
        {
            if (editingRollUpKey == rollUp.Key) editError = true;
        }
        finally { editSaving = false; }
    }
}
