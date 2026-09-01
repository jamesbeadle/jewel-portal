using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Components;

public partial class ValuationReportTable
{
    // ---- % complete click-to-edit -----------------------------------------
    // One cell edits at a time. Enter or the tick saves; Esc or the cross discards;
    // and clicking anywhere else saves too, if the value changed and parses — a QS who
    // types a %, scrolls a hundred rows and clicks the next cell has plainly finished
    // with the first one, and losing that keystroke silently is how wrong claims go out.
    // An UNCHANGED value on click-away just closes the editor; an invalid one stays
    // open with the error ring (garbage is never committed by a stray click).
    // Enter additionally advances to the next line's % editor (the tick keeps
    // save-and-close), so a claim can be keyed top-to-bottom: type, Enter, type, Enter…
    private string? editingLineId;
    private string editValue = "";
    private bool editError;
    private bool editSaving;
    private bool focusPending;
    private ElementReference editInput;

    private void StartEdit(ValuationLineItem line)
    {
        CancelRollUpEdit();
        RevealRollUpFor(line); // a line inside a collapsed consolidated row must be visible to edit
        editingLineId = line.ValuationLineItemId;
        editValue = PercentFor(line).ToString("0.##", Gb);
        editError = false;
        focusPending = true;
    }

    private void CancelEdit()
    {
        editingLineId = null;
        editingRollUpKey = null;
        editError = false;
    }

    private async Task OnEditKeyDownAsync(KeyboardEventArgs e, ValuationLineItem line)
    {
        if (e.Key == "Enter") await CommitEditAsync(line, advanceToNext: true);
        else if (e.Key == "Escape") CancelEdit();
    }

    private async Task CommitEditAsync(ValuationLineItem line, bool advanceToNext = false)
    {
        if (editSaving) return; // a second Enter while the save is in flight must not double-post
        if (SelectedClaim is null) { CancelEdit(); return; }
        if (!decimal.TryParse(editValue.Trim().TrimEnd('%'), System.Globalization.NumberStyles.Number, Gb, out var percent)
            || !WithinBounds(line, percent))
        {
            editError = true;
            return;
        }
        // Unchanged value: nothing to record, so Enter just moves on — lines that are
        // already right can be skimmed past without a round-trip per row.
        if (percent == PercentFor(line))
        {
            if (advanceToNext) AdvanceToNextEditable(line);
            else CancelEdit();
            return;
        }
        editSaving = true;
        try
        {
            await Store.RecordEntryAsync(ProjectId, new RecordClaimEntry(SelectedClaim.ValuationClaimId, line.ValuationLineItemId, percent));
            editingLineId = null;
            editError = false;
            OnParametersSet();
            if (advanceToNext) AdvanceToNextEditable(line);
        }
        catch
        {
            // Save failed (validation or the API being unreachable): keep the editor open
            // with the typed value and flag it, instead of crashing the circuit.
            editError = true;
        }
        finally { editSaving = false; }
    }

    // Save-on-blur: the input losing focus commits a changed, valid value. The click that
    // caused the blur may be the very thing that moves the editor on (another row's %
    // button, "Bulk edit %"), and Blazor dispatches that click while our save is still in
    // flight — so after the await, only close the editor if it is still this line's.
    // Stray blurs from an editor that has already moved on or closed (Enter-advance and
    // Esc both remove the input, which can fire one) are filtered by the same check up top.
    private async Task OnEditBlurAsync(ValuationLineItem line)
    {
        if (editSaving || editingLineId != line.ValuationLineItemId) return;
        if (SelectedClaim is null) { CancelEdit(); return; }
        if (!TryParsePercent(editValue, out var percent) || !WithinBounds(line, percent))
        {
            // Nothing safe to save — keep the editor open showing the error ring. If the
            // user has clicked another cell's %, that editor opens and this text is
            // discarded: a click-away never commits a value that doesn't parse.
            editError = true;
            return;
        }
        if (percent == PercentFor(line)) { CancelEdit(); return; }

        editSaving = true;
        try
        {
            await Store.RecordEntryAsync(ProjectId, new RecordClaimEntry(SelectedClaim.ValuationClaimId, line.ValuationLineItemId, percent));
            if (editingLineId == line.ValuationLineItemId)
            {
                editingLineId = null;
                editError = false;
            }
            // If the blur was "Bulk edit %", StartBulkEdit snapshotted this line's % before
            // the save landed — refresh that one entry so Save all can't post the stale
            // value back over what was just committed.
            if (bulkEditing && bulkValues.ContainsKey(line.ValuationLineItemId))
                bulkValues[line.ValuationLineItemId] = percent.ToString("0.##", Gb);
            OnParametersSet();
        }
        catch
        {
            // Same stance as CommitEditAsync: flag rather than crash — but only if the
            // editor hasn't already moved on to another line while the save was in flight.
            if (editingLineId == line.ValuationLineItemId) editError = true;
        }
        finally { editSaving = false; }
    }

    // Enter-to-advance: open the % editor on the next counting line below, in rendered
    // order (sections in their fixed order, lines by DisplayOrder), rolling over into the
    // following section — auto-expanding it if collapsed — so the whole bill can be keyed
    // in one pass. On the last line there is nothing to advance to: the save above stands
    // and the editor simply closes.
    private void AdvanceToNextEditable(ValuationLineItem current)
    {
        var ordered = Sections.SelectMany(section => section.Lines)
                              .Where(l => l.CountsTowardTotals)
                              .ToList();
        var index = ordered.FindIndex(l => l.ValuationLineItemId == current.ValuationLineItemId);
        if (index < 0 || index + 1 >= ordered.Count) { CancelEdit(); return; }

        var next = ordered[index + 1];
        expanded.Add(next.ElementType);
        StartEdit(next);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (focusPending && (editingLineId is not null || editingRollUpKey is not null))
        {
            focusPending = false;
            await editInput.FocusAsync();
        }
        if (reviseFocusPending && revisingVoLineId is not null)
        {
            reviseFocusPending = false;
            await reviseInput.FocusAsync();
        }
    }

    private async Task RemoveLineAsync(ValuationLineItem line)
    {
        await Store.RemoveLineAsync(ProjectId, line.ValuationLineItemId);
        OnParametersSet();
    }

}
