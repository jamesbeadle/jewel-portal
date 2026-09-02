using System.Text.Json;

namespace Jewel.JPMS.Components;

// The dialog ⇄ assistant pipe (the work_order_edit AiTask). Out: SerialiseState is republished
// by the hosting modal on every edit (AiTaskState.UpdateDraft) — the model always reasons from
// what is on screen NOW, payment locks included. In: ApplyAssistant merges the model's
// proposals — a field it did not send keeps what the user typed, and the lines it sends are
// MATCHED back to the rows on screen by title so a kept line's identity (and the paid-to-date
// history hanging off it) survives the proposal.
public partial class WorkOrderForm
{
    // The manual_timesheet siteName rule, applied to suppliers: the model passes the NAME through
    // and the form does the matching against the live directory — an invented id would raise an
    // order to the wrong firm. An unmatched name is said out loud under the picker and in the
    // republished state, so both the user and the model can see the choice still stands open.
    private string? assistantSupplierNote;

    /// <summary>The form's live state as the JSON the assistant task carries with every turn.</summary>
    public string SerialiseState() => JsonSerializer.Serialize(new
    {
        supplier = SelectedSupplier?.CompanyName ?? "",
        supplierUnmatched = assistantSupplierNote,
        title,
        scope,
        saveAsDraft,
        lines = lines.Select(line => new
        {
            title = line.Title,
            description = line.Description,
            costCode = line.CostCode,
            // NUMBERS, as the schema declares them — null while blank or unparseable. Serialising
            // the raw text ("" included) taught the model the wrong shape.
            quantity = decimal.TryParse(line.QuantityText, out var lineQuantity) ? (decimal?)lineQuantity : null,
            unit = line.Unit,
            unitCost = decimal.TryParse(line.UnitCostText, out var lineRate) ? (decimal?)lineRate : null,
            amount = decimal.TryParse(line.AmountText, out var lineAmount) ? (decimal?)lineAmount : null,
            // Says which lines are anchored: a paid line can't be removed and can't drop below this.
            paidToDate = line.PaidToDate
        })
    });

    /// <summary>
    /// The assistant's proposals, merged in. The lines array is the schedule as it should stand:
    /// each proposed line is matched to an existing row by title (case-insensitive) so the row
    /// keeps its WorkOrderLineId and payment history; unmatched proposals become new rows. A paid
    /// row the proposal dropped is kept anyway — the API would refuse its removal, and a silent
    /// drop here would turn that refusal into a mystery. Validation is unchanged: whatever lands
    /// here still passes CanSave/TryBuildDraft when the user presses the button.
    /// </summary>
    public void ApplyAssistant(string fieldsJson)
    {
        if (string.IsNullOrWhiteSpace(fieldsJson)) return;
        try
        {
            using var document = JsonDocument.Parse(fieldsJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;

            ApplyOrderProposal(root);
            if (root.TryGetProperty("lines", out var proposedLines) && proposedLines.ValueKind == JsonValueKind.Array)
                ApplyLinesProposal(proposedLines);

            StateHasChanged();
            if (OnChanged.HasDelegate) _ = OnChanged.InvokeAsync();
        }
        catch (JsonException)
        {
            // A malformed proposal is the model's problem, not the user's. The form stands.
        }
    }

    private void ApplyOrderProposal(JsonElement root)
    {
        // Create only: work_order_edit's schema doesn't declare supplier, so a model that
        // echoes state back must never re-point an ISSUED order at another firm.
        if (!IsEditing
            && ReadText(root, "supplier") is { } proposedSupplier
            && !string.IsNullOrWhiteSpace(proposedSupplier))
        {
            ApplySupplierProposal(proposedSupplier);
        }
        // Whitespace-only counts as not sent — the schema promises "leave it out to keep
        // what the dialog already shows", and an empty string used to wipe the user's text.
        if (ReadText(root, "title") is { } proposedTitle && !string.IsNullOrWhiteSpace(proposedTitle))
            title = proposedTitle;
        if (ReadText(root, "scope") is { } proposedScope && !string.IsNullOrWhiteSpace(proposedScope))
            scope = proposedScope;
        if (root.TryGetProperty("saveAsDraft", out var proposedDraft)
            && proposedDraft.ValueKind is JsonValueKind.True or JsonValueKind.False
            && !DraftTickLocked && !IsEditing)
        {
            saveAsDraft = proposedDraft.ValueKind == JsonValueKind.True;
        }
    }

    private void ApplyLinesProposal(JsonElement proposedLines)
    {
        var unmatched = new List<LineRow>(lines);
        var next = new List<LineRow>();
        foreach (var element in proposedLines.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object) continue;
            var lineTitle = ReadText(element, "title") ?? "";
            if (string.IsNullOrWhiteSpace(lineTitle)) continue;
            var match = unmatched.FirstOrDefault(row =>
                !string.IsNullOrWhiteSpace(row.Title)
                && string.Equals(row.Title.Trim(), lineTitle.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is not null) unmatched.Remove(match);
            var row = match ?? new LineRow();
            row.Title = lineTitle;
            ApplyLineProposal(row, element);
            next.Add(row);
        }
        foreach (var anchored in unmatched.Where(row => row.PaidToDate != 0m)) next.Add(anchored);
        if (next.Count > 0) lines = next;
    }

    private static void ApplyLineProposal(LineRow row, JsonElement element)
    {
        if (ReadText(element, "description") is { } lineDescription) row.Description = lineDescription;
        if (ReadText(element, "costCode") is { } lineCode && !string.IsNullOrWhiteSpace(lineCode)) row.CostCode = lineCode;
        // The measured breakdown, when the model sends one: quantity and unitCost land
        // in their own boxes and the amount derives from them (RecalculateAmount), so
        // "14 m2 @ £54.00" prints as columns rather than description prose.
        if (ReadNumberText(element, "quantity") is { } lineQuantity) row.QuantityText = lineQuantity;
        if (ReadText(element, "unit") is { } lineUnit && !string.IsNullOrWhiteSpace(lineUnit)) row.Unit = lineUnit.Trim();
        if (ReadNumberText(element, "unitCost") is { } lineRate) row.UnitCostText = lineRate;
        if (ReadNumberText(element, "amount") is { } lineAmount) row.AmountText = lineAmount;
        RecalculateAmount(row);
    }

    private void ApplySupplierProposal(string proposed)
    {
        var candidates = Subcontractors.Current ?? Array.Empty<Subcontractor>();
        var trimmed = proposed.Trim();
        var match = candidates.FirstOrDefault(sub =>
                        string.Equals(sub.CompanyName.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                    ?? candidates.FirstOrDefault(sub =>
                        sub.CompanyName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                        || trimmed.Contains(sub.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase));
        subcontractorId = match?.SubcontractorId ?? subcontractorId;
        assistantSupplierNote = match is null
            ? $"\"{trimmed}\" isn't in the subcontractor directory — pick the supplier from the list."
            : null;
    }

    private static string? ReadText(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? ReadNumberText(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDecimal().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonValueKind.String when !string.IsNullOrWhiteSpace(value.GetString()) => value.GetString(),
            _ => null
        };
    }
}
