using System.Text.Json;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Components;

public partial class WorkOrderForm : IDisposable
{
    /// <summary>The core order as it would be raised — the host sends CreateManualWorkOrder now
    /// (the modal) or stages it to fire with the email's Apply (System Actions).</summary>
    public sealed record Draft(
        string SubcontractorId,
        string Title,
        string Scope,
        IReadOnlyList<ManualWorkOrderLine> Lines,
        DateTimeOffset? ProgrammeStart,
        DateTimeOffset? TargetCompletion,
        string ProgrammeNotes,
        bool SaveAsDraft,
        bool DepositRequired,
        decimal? DepositPercent);

    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public bool Busy { get; set; }

    /// <summary>The order being edited (the modal's edit mode) — null when raising new.</summary>
    [Parameter] public ProjectWorkOrderDetail? Editing { get; set; }

    /// <summary>The modal locks the draft tick once the order has saved (package-retry path) —
    /// "save as draft" would then be a claim the save can't keep.</summary>
    [Parameter] public bool DraftTickLocked { get; set; }

    /// <summary>Raised when anything affecting the host changes (order total for the packaging
    /// step, the supplier for the PO email, the draft tick that hides packaging).</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    public sealed class LineRow
    {
        // Ties the row to an existing line so its paid-to-date and invoice history survive the
        // edit; null for rows added in this session.
        public string? WorkOrderLineId { get; set; }
        public decimal PaidToDate { get; set; }
        public string CostCode { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string AmountText { get; set; } = "";
        // The measured breakdown ("14" / "m2" / "54.00"): all optional, but quantity and rate
        // come as a pair — when both parse, the amount is DERIVED (qty × rate) and its input
        // locks, so the printed Qty/Unit, Unit Cost and Price columns can never disagree.
        public string QuantityText { get; set; } = "";
        public string Unit { get; set; } = "";
        public string UnitCostText { get; set; } = "";
    }

    /// <summary>True when the line carries a full measured breakdown — a positive quantity and a
    /// parseable rate. Only then do quantity, unit and rate travel to the server; a lone
    /// quantity or lone rate is a validation problem, not a silent "1 item".</summary>
    internal static bool IsMeasured(LineRow line) =>
        Parse(line.QuantityText) is { } quantity && quantity > 0m && Parse(line.UnitCostText) is not null;

    /// <summary>Qty × rate, kept in the amount box whenever both halves parse.</summary>
    private static void RecalculateAmount(LineRow line)
    {
        if (Parse(line.QuantityText) is { } quantity && quantity > 0m
            && Parse(line.UnitCostText) is { } rate)
        {
            line.AmountText = Math.Round(quantity * rate, 2)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private string subcontractorId = "";
    private string title = "";
    private string scope = "";
    private DateTime? programmeStart;
    private DateTime? targetCompletion;
    private string programmeNotes = "";
    private List<LineRow> lines = new() { new LineRow() };
    private bool depositRequired;
    private string depositPercentText = "";
    private bool saveAsDraft;
    private string? error;
    private bool seeded;

    private bool IsEditing => Editing is not null;

    // ---- What the hosts read ----

    public bool SaveAsDraft => saveAsDraft;

    public decimal OrderTotal => lines.Sum(line => Parse(line.AmountText) ?? 0m);

    /// <summary>The chosen supplier's directory record — where a released order's PO email goes.</summary>
    public Subcontractor? SelectedSupplier =>
        subcontractorId == ""
            ? null
            : (Subcontractors.Current ?? Array.Empty<Subcontractor>()).FirstOrDefault(sub =>
                string.Equals(sub.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase));

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
            // Quantity and rate are a pair: one without the other would print as "1 item" while
            // looking measured on screen — refuse rather than mislead.
            foreach (var line in filledLines)
            {
                var hasQuantity = !string.IsNullOrWhiteSpace(line.QuantityText);
                var hasRate = !string.IsNullOrWhiteSpace(line.UnitCostText);
                if (hasQuantity != hasRate)
                    return $"\"{Truncate(line.Title, 40)}\" — give a quantity AND a unit rate together, or neither.";
                if (hasQuantity && (Parse(line.QuantityText) is not { } quantity || quantity <= 0m))
                    return $"\"{Truncate(line.Title, 40)}\" — the quantity must be a number above zero.";
                if (hasRate && Parse(line.UnitCostText) is null)
                    return $"\"{Truncate(line.Title, 40)}\" — the unit rate isn't a number.";
            }
            if (depositRequired && (Parse(depositPercentText) is not { } depositPercent
                                    || depositPercent <= 0m || depositPercent > 100m))
                return "A required deposit needs a percentage above 0 and no more than 100.";
            // A paid line anchors the Financials tab's paid figures: its amount can change, but
            // never below what has already been paid against it.
            foreach (var line in filledLines.Where(line => line.PaidToDate != 0m))
            {
                if (Parse(line.AmountText) is { } amount && Math.Abs(amount) < Math.Abs(line.PaidToDate))
                    return $"\"{Truncate(line.Title, 40)}\" — {Money(line.PaidToDate)} has been paid against this line; its amount can't drop below that.";
            }
            return null;
        }
    }

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

    public void ShowError(string message)
    {
        error = message;
        StateHasChanged();
    }

    // ---- The dialog ⇄ assistant pipe (the work_order_edit AiTask) --------------------------------
    // Out: SerialiseState is republished by the hosting modal on every edit (AiTaskState.UpdateDraft)
    // — the model always reasons from what is on screen NOW, payment locks included. In:
    // ApplyAssistant merges the model's proposals — a field it did not send keeps what the user
    // typed, and the lines it sends are MATCHED back to the rows on screen by title so a kept
    // line's identity (and the paid-to-date history hanging off it) survives the proposal.

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

            if (root.TryGetProperty("lines", out var proposedLines) && proposedLines.ValueKind == JsonValueKind.Array)
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
                    next.Add(row);
                }
                foreach (var anchored in unmatched.Where(row => row.PaidToDate != 0m)) next.Add(anchored);
                if (next.Count > 0) lines = next;
            }

            StateHasChanged();
            if (OnChanged.HasDelegate) _ = OnChanged.InvokeAsync();
        }
        catch (JsonException)
        {
            // A malformed proposal is the model's problem, not the user's. The form stands.
        }
    }

    // The manual_timesheet siteName rule, applied to suppliers: the model passes the NAME through
    // and the form does the matching against the live directory — an invented id would raise an
    // order to the wrong firm. An unmatched name is said out loud under the picker and in the
    // republished state, so both the user and the model can see the choice still stands open.
    private string? assistantSupplierNote;

    private void ApplySupplierProposal(string proposed)
    {
        var candidates = Subcontractors.Current ?? Array.Empty<Subcontractor>();
        var trimmed = proposed.Trim();
        var match = candidates.FirstOrDefault(sub =>
                        string.Equals(sub.CompanyName.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                    ?? candidates.FirstOrDefault(sub =>
                        sub.CompanyName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                        || trimmed.Contains(sub.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            subcontractorId = match.SubcontractorId;
            assistantSupplierNote = null;
        }
        else
        {
            assistantSupplierNote = $"\"{trimmed}\" isn't in the subcontractor directory — pick the supplier from the list.";
        }
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

    // ---- Lifecycle ----

    protected override async Task OnInitializedAsync()
    {
        Subcontractors.OnChanged += StoreChanged;
        CostCenters.OnChanged += StoreChanged;
        Seed();
        // Freshen everything the pickers show; cached values render immediately.
        await Task.WhenAll(
            Subcontractors.RefreshAsync(CancellationToken.None),
            CostCenters.RefreshAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        Subcontractors.OnChanged -= StoreChanged;
        CostCenters.OnChanged -= StoreChanged;
    }

    private void StoreChanged() => InvokeAsync(StateHasChanged);

    private void Seed()
    {
        if (seeded) return;
        seeded = true;
        if (Editing is null) return;
        subcontractorId = Editing.Order.SubcontractorId;
        title = Editing.Order.Title;
        scope = Editing.Order.Scope;
        programmeStart = Editing.Order.ProgrammeStart?.LocalDateTime.Date;
        targetCompletion = Editing.Order.ScheduledCompletion?.LocalDateTime.Date;
        programmeNotes = Editing.Order.ProgrammeNotes;
        lines = Editing.Lines
            .OrderBy(line => line.SortOrder)
            .Select(line => new LineRow
            {
                WorkOrderLineId = line.WorkOrderLineId,
                PaidToDate = line.PaidToDate,
                CostCode = line.CostCode,
                Title = line.Title,
                Description = line.Description,
                AmountText = line.LineTotal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                // A real measured breakdown round-trips into the qty/unit/rate boxes; the
                // long-standing "1 item" placeholder stays out of them — showing it would
                // dress every legacy line up as measured.
                QuantityText = line.Quantity == 1m && line.Unit == "item"
                    ? ""
                    : line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Unit = line.Quantity == 1m && line.Unit == "item" ? "" : line.Unit,
                UnitCostText = line.Quantity == 1m && line.Unit == "item"
                    ? ""
                    : line.UnitCost.ToString(System.Globalization.CultureInfo.InvariantCulture)
            })
            .ToList();
        if (lines.Count == 0) lines = new List<LineRow> { new LineRow() };
        depositRequired = Editing.Order.DepositRequired;
        depositPercentText = Editing.Order.DepositPercent?
            .ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
    }

    /// <summary>Back to a blank form — the pane calls this after staging an order.</summary>
    public void Reset()
    {
        subcontractorId = title = scope = programmeNotes = depositPercentText = "";
        programmeStart = targetCompletion = null;
        lines = new List<LineRow> { new LineRow() };
        depositRequired = saveAsDraft = false;
        error = null;
        StateHasChanged();
    }

    // ---- Field plumbing (OnChanged keeps the modal's packaging totals live) ----

    private IEnumerable<Subcontractor> SortedSubcontractors =>
        (Subcontractors.Current ?? Array.Empty<Subcontractor>())
            .OrderBy(sub => sub.CompanyName, StringComparer.OrdinalIgnoreCase);

    private List<LineRow> EnteredLines() =>
        lines.Where(line => line.CostCode != "" && Parse(line.AmountText) is { } amount && amount != 0m).ToList();

    private async Task SetSubcontractorAsync(string? value)
    {
        subcontractorId = value ?? "";
        assistantSupplierNote = null; // the user's own pick settles it
        await OnChanged.InvokeAsync();
    }

    private async Task SetSaveAsDraftAsync(ChangeEventArgs e)
    {
        saveAsDraft = e.Value is true;
        await OnChanged.InvokeAsync();
    }

    private async Task AddLineAsync()
    {
        lines.Add(new LineRow());
        await OnChanged.InvokeAsync();
    }

    private async Task RemoveLineAsync(int index)
    {
        lines.RemoveAt(index);
        await OnChanged.InvokeAsync();
    }

    // @bind:after on the title and scope inputs — the assistant task republishes the draft on
    // OnChanged, and those two fields travel in it, so their edits must raise it too.
    private Task NotifyChangedAsync() => OnChanged.InvokeAsync();

    private void SetLineCode(LineRow line, string? value) { line.CostCode = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineTitle(LineRow line, string? value) { line.Title = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineDescription(LineRow line, string? value) { line.Description = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineAmount(LineRow line, string? value) { line.AmountText = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineQuantity(LineRow line, string? value) { line.QuantityText = value ?? ""; RecalculateAmount(line); _ = OnChanged.InvokeAsync(); }
    private void SetLineUnit(LineRow line, string? value) { line.Unit = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineUnitCost(LineRow line, string? value) { line.UnitCostText = value ?? ""; RecalculateAmount(line); _ = OnChanged.InvokeAsync(); }
    private void SetDepositPercent(string? value) { depositPercentText = value ?? ""; _ = OnChanged.InvokeAsync(); }

    // ---- Cost-centre picker options (typed-to-find; cached against the master list) ----

    private IReadOnlyList<SearchSelect.Option>? costCentreOptionsCache;
    private object? costCentreOptionsCacheKey;

    private IReadOnlyList<SearchSelect.Option> CostCentreOptions
    {
        get
        {
            var centres = CostCenters.Alphabetical;
            if (costCentreOptionsCache is null || !ReferenceEquals(costCentreOptionsCacheKey, centres))
            {
                costCentreOptionsCache = centres
                    .Select(centre => new SearchSelect.Option(centre.Code, $"{centre.Code} {centre.Name}"))
                    .ToList();
                costCentreOptionsCacheKey = centres;
            }
            return costCentreOptionsCache;
        }
    }

    // An order being edited can carry a code the master no longer offers — lead the list with the
    // stored value so the picker shows the code the line actually sits on.
    private IReadOnlyList<SearchSelect.Option> CostCentreOptionsFor(LineRow line)
    {
        var options = CostCentreOptions;
        if (line.CostCode == "" || options.Any(option => option.Value == line.CostCode)) return options;
        var master = options.FirstOrDefault(option =>
            string.Equals(option.Value, line.CostCode, StringComparison.OrdinalIgnoreCase));
        var withCurrent = new List<SearchSelect.Option>(options.Count + 1)
        {
            new(line.CostCode, master?.Label ?? $"{line.CostCode} (retired)")
        };
        withCurrent.AddRange(options.Where(option => !ReferenceEquals(option, master)));
        return withCurrent;
    }

    // ---- Small shared helpers ----

    internal static decimal? Parse(string text) =>
        decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static DateTimeOffset? AsUtcDate(DateTime? date) =>
        date is null ? null : new DateTimeOffset(DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc));

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    internal static string Money(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
}
