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

    public bool SaveAsDraft => saveAsDraft;

    public decimal OrderTotal => lines.Sum(line => Parse(line.AmountText) ?? 0m);

    /// <summary>The chosen supplier's directory record — where a released order's PO email goes.</summary>
    public Subcontractor? SelectedSupplier =>
        subcontractorId == ""
            ? null
            : (Subcontractors.Current ?? Array.Empty<Subcontractor>()).FirstOrDefault(sub =>
                string.Equals(sub.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase));

    public void ShowError(string message)
    {
        error = message;
        StateHasChanged();
    }

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
        lines = Editing.Lines.OrderBy(line => line.SortOrder).Select(LineRow.From).ToList();
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
}
