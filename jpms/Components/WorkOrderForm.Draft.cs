namespace Jewel.JPMS.Components;

public partial class WorkOrderForm
{
    public bool CanSave =>
        subcontractorId != ""
        && !string.IsNullOrWhiteSpace(title)
        && lines.Any(line => line.CostCode != "" && Parse(line.AmountText) is { } amount && amount != 0m)
        && ValidationProblem is null;

    /// <summary>What still stops the order saving — null when it is complete. The core rules only;
    /// the modal layers its packaging checks on top.</summary>
    public string? ValidationProblem
    {
        get
        {
            var filledLines = lines.Where(line =>
                line.CostCode != "" || !string.IsNullOrWhiteSpace(line.Title) || !string.IsNullOrWhiteSpace(line.AmountText)).ToList();
            if (filledLines.Any(line => line.CostCode == "")) return "Choose a cost centre for every line.";
            if (filledLines.Any(line => string.IsNullOrWhiteSpace(line.Title))) return "Every line needs a title.";
            if (filledLines.Any(line => Parse(line.AmountText) is not { } amount || amount == 0m))
                return "Every line needs a non-zero amount.";
            foreach (var line in filledLines)
            {
                if (MeasuredBreakdownProblem(line) is { } problem) return problem;
            }
            if (depositRequired && (Parse(depositPercentText) is not { } depositPercent
                                    || depositPercent <= 0m || depositPercent > 100m))
                return "A required deposit needs a percentage above 0 and no more than 100.";
            foreach (var line in filledLines.Where(line => line.PaidToDate != 0m))
            {
                if (PaidLineProblem(line) is { } problem) return problem;
            }
            return null;
        }
    }

    // Quantity and rate are a pair: one without the other would print as "1 item" while
    // looking measured on screen — refuse rather than mislead.
    private static string? MeasuredBreakdownProblem(LineRow line)
    {
        var hasQuantity = !string.IsNullOrWhiteSpace(line.QuantityText);
        var hasRate = !string.IsNullOrWhiteSpace(line.UnitCostText);
        if (hasQuantity != hasRate)
            return $"\"{Truncate(line.Title, 40)}\" — give a quantity AND a unit rate together, or neither.";
        if (hasQuantity && (Parse(line.QuantityText) is not { } quantity || quantity <= 0m))
            return $"\"{Truncate(line.Title, 40)}\" — the quantity must be a number above zero.";
        if (hasRate && Parse(line.UnitCostText) is null)
            return $"\"{Truncate(line.Title, 40)}\" — the unit rate isn't a number.";
        return null;
    }

    // A paid line anchors the Financials tab's paid figures: its amount can change, but
    // never below what has already been paid against it.
    private static string? PaidLineProblem(LineRow line) =>
        Parse(line.AmountText) is { } amount && Math.Abs(amount) < Math.Abs(line.PaidToDate)
            ? $"\"{Truncate(line.Title, 40)}\" — {Money(line.PaidToDate)} has been paid against this line; its amount can't drop below that."
            : null;

    /// <summary>The valid core order, or null with the problem shown inline.</summary>
    public Draft? TryBuildDraft()
    {
        error = null;
        if (subcontractorId == "") { error = "Choose the subcontractor the order is raised to."; return null; }
        if (string.IsNullOrWhiteSpace(title)) { error = "Give the work order a title."; return null; }
        if (ValidationProblem is { } problem) { error = problem; return null; }
        var orderLines = EnteredLines()
            .Select(line => new ManualWorkOrderLine(
                line.CostCode, line.Title.Trim(), Parse(line.AmountText)!.Value, line.Description.Trim(),
                Quantity: IsMeasured(line) ? Parse(line.QuantityText) : null,
                Unit: IsMeasured(line) ? line.Unit.Trim() : "",
                UnitCost: IsMeasured(line) ? Parse(line.UnitCostText) : null))
            .ToList();
        if (orderLines.Count == 0) { error = "Add at least one priced line."; return null; }
        StateHasChanged();
        return new Draft(
            subcontractorId, title.Trim(), scope.Trim(), orderLines,
            AsUtcDate(programmeStart), AsUtcDate(targetCompletion), programmeNotes.Trim(),
            SaveAsDraft: saveAsDraft,
            DepositRequired: depositRequired,
            DepositPercent: depositRequired ? Parse(depositPercentText) : null);
    }

    /// <summary>The edit path's lines — same filter as the draft, with the row identities the
    /// paid-to-date history hangs off.</summary>
    public IReadOnlyList<UpdatedManualWorkOrderLine> BuildEditedLines() =>
        EnteredLines()
            .Select(line => new UpdatedManualWorkOrderLine(
                line.WorkOrderLineId, line.CostCode, line.Title.Trim(), Parse(line.AmountText)!.Value,
                line.Description.Trim(),
                Quantity: IsMeasured(line) ? Parse(line.QuantityText) : null,
                Unit: IsMeasured(line) ? line.Unit.Trim() : "",
                UnitCost: IsMeasured(line) ? Parse(line.UnitCostText) : null))
            .ToList();

    private List<LineRow> EnteredLines() =>
        lines.Where(line => line.CostCode != "" && Parse(line.AmountText) is { } amount && amount != 0m).ToList();

    private static DateTimeOffset? AsUtcDate(DateTime? date) =>
        date is null ? null : new DateTimeOffset(DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc));

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
