using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.MoneyFormats;


namespace Jewel.JPMS.Components;

public partial class ValuationInvoicesSection
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    // When a claim is selected on the report, new invoices are drawn against it.
    [Parameter] public string? ValuationClaimId { get; set; }
    [Parameter] public EventCallback<decimal> OnInvoicedToDateChanged { get; set; }

    /// <summary>Raised after a change that moves "Certified to date" (issue, delete,
    /// manual add/edit). The server re-freezes any Preapproved claim's totals at the same
    /// time, so the parent should refresh its claims to show them.</summary>
    [Parameter] public EventCallback OnCertifiedChanged { get; set; }

    /// <summary>Opens the read-only report-snapshot viewer for the given snapshot id —
    /// "show me the valuation report behind this invoice".</summary>
    [Parameter] public EventCallback<string> OnViewSnapshot { get; set; }

    private static readonly System.Globalization.CultureInfo Gb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");

    private bool isOpen;
    private bool busy;
    private string? error;
    private IReadOnlyList<ValuationInvoice> invoices = Array.Empty<ValuationInvoice>();

    private string newMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");
    private string newAmount = "";
    // Historic entry: recorded directly as Issued/Paid, backdated to the period month.
    private bool newIsHistoric;
    private string newPaidAmount = "";
    private string newNote = "";

    private ValuationInvoice? paymentInvoice;
    private string paymentAmount = "";
    private string? pendingDeleteId;

    // The invoice whose Actions menu is currently open — while set, the table's overflow cap is
    // lifted so the menu isn't clipped (see the wrapper div). Cleared on reload in case the row
    // disappears under an open menu.
    private string? openMenuId;

    private ValuationInvoice? rejectInvoice;
    private string rejectReason = "";

    private ValuationInvoice? editInvoice;
    private string editMonth = "";
    private string editAmount = "";
    private string editPaidAmount = "";
    private string editNote = "";

    private string? historyOpenId;
    private IReadOnlyList<ValuationInvoiceEvent>? historyEvents;

    private int ColumnCount => CanManage ? 7 : 6;

    // The row's Actions menu: the stage's own moves first, then amending/withdrawing, then the
    // record (history, delete). Same gates and hover text as the old inline links, one list.
    private List<DropdownMenu.Item> InvoiceMenuItems(ValuationInvoice invoice)
    {
        var items = new List<DropdownMenu.Item>();

        // Group 0 — moving the invoice along its lifecycle.
        if (!invoice.IsManual && invoice.Status == ValuationInvoiceStatus.Raised)
        {
            items.Add(new(Label: "Send claim",
                OnSelect: EventCallback.Factory.Create(this, () => SubmitAsync(invoice)),
                Hint: "Send to the architect/client for approval"));
            items.Add(new(Label: "Issue without approval",
                OnSelect: EventCallback.Factory.Create(this, () => IssueAsync(invoice)),
                Hint: "Skip the approval loop — counts toward certified to date"));
        }
        else if (invoice.Status == ValuationInvoiceStatus.Submitted)
        {
            items.Add(new(Label: "Record approval",
                OnSelect: EventCallback.Factory.Create(this, () => ApproveAsync(invoice)),
                Hint: "Record the client's approval — issue next to count toward certified to date"));
            items.Add(new(Label: "Record rejection…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenReject(invoice)),
                Hint: "Record the client's rejection — the invoice unlocks for amendment or cancellation"));
            items.Add(new(Label: "Issue without approval",
                OnSelect: EventCallback.Factory.Create(this, () => IssueAsync(invoice)),
                Hint: "For clients with no formal approval loop — counts toward certified to date"));
        }
        else if (invoice.Status == ValuationInvoiceStatus.Approved)
        {
            items.Add(new(Label: "Issue invoice",
                OnSelect: EventCallback.Factory.Create(this, () => IssueAsync(invoice)),
                Hint: "Counts toward certified to date"));
        }
        else if (invoice.Status == ValuationInvoiceStatus.Issued)
        {
            items.Add(new(Label: "Record payment…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenPayment(invoice)),
                Hint: "Record the client's payment when the cash lands"));
        }

        // Group 1 — amending and withdrawing.
        if (invoice.IsEditable && invoice.Status != ValuationInvoiceStatus.Cancelled)
            items.Add(new(Label: invoice.Status == ValuationInvoiceStatus.Rejected ? "Amend…" : "Edit…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenEdit(invoice)),
                Hint: invoice.Status == ValuationInvoiceStatus.Rejected
                    ? "Amend and return to draft, ready to send again"
                    : "Amend period/amount",
                Group: 1));
        if (!invoice.IsManual && invoice.Status is ValuationInvoiceStatus.Raised or ValuationInvoiceStatus.Rejected)
            items.Add(new(Label: "Cancel invoice",
                OnSelect: EventCallback.Factory.Create(this, () => CancelAsync(invoice)),
                Hint: "Withdraw — kept for the audit trail, excluded from every total",
                Destructive: true, Group: 1));

        // Group 2 — the record.
        items.Add(new(Label: historyOpenId == invoice.ValuationInvoiceId ? "Hide history" : "History",
            OnSelect: EventCallback.Factory.Create(this, () => ToggleHistoryAsync(invoice)),
            Group: 2));
        items.Add(new(Label: "Delete…",
            OnSelect: EventCallback.Factory.Create(this, () => pendingDeleteId = invoice.ValuationInvoiceId),
            Hint: invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid
                ? "Delete this invoice — certified to date (and a paid one's receipts) roll back"
                : "Delete this invoice",
            Destructive: true, Group: 2));

        return items;
    }

    private bool CanManage => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    // Cancelled invoices stay listed (audit trail) but never count.
    private IReadOnlyList<ValuationInvoice> LiveInvoices =>
        invoices.Where(invoice => invoice.Status != ValuationInvoiceStatus.Cancelled).ToList();

    // Issued + Paid invoices are what the client has actually been invoiced — this is the
    // figure that feeds "Certified to date" on the report summary.
    private decimal InvoicedToDate => LiveInvoices
        .Where(invoice => invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid)
        .Sum(invoice => invoice.Amount);

    private decimal TotalPaid => LiveInvoices.Sum(invoice => invoice.AmountPaid);

    // Submitted + Approved: claimed from the client but not yet certifiable.
    private decimal AwaitingApproval => LiveInvoices
        .Where(invoice => invoice.IsAwaitingApproval)
        .Sum(invoice => invoice.Amount);

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private void Toggle() => isOpen = !isOpen;

    /// <summary>Re-pulls the invoice list. Public so the page can nudge it after an
    /// out-of-band change — e.g. deleting a snapshot clears invoice links server-side.</summary>
    public async Task ReloadAsync()
    {
        openMenuId = null;
        try
        {
            invoices = await Invoices.ListAsync(ProjectId);
            error = null;
        }
        catch { error = "Couldn't load valuation invoices. Please try again."; }
        await OnInvoicedToDateChanged.InvokeAsync(InvoicedToDate);
        StateHasChanged();
    }

    /// <summary>Open this section's modals for a given invoice from outside — the claim card
    /// on the valuation page drives the happy path and borrows these forms rather than
    /// duplicating them. The modals render whether or not the accordion is open.</summary>
    public void OpenPaymentFor(ValuationInvoice invoice) { OpenPayment(invoice); StateHasChanged(); }
    public void OpenRejectFor(ValuationInvoice invoice) { OpenReject(invoice); StateHasChanged(); }
    public void OpenEditFor(ValuationInvoice invoice) { OpenEdit(invoice); StateHasChanged(); }

    private async Task AddAsync()
    {
        if (busy) return;
        error = null;
        // Historic convenience: if only the Paid box was filled in, that figure IS the
        // invoice amount (a fully paid historic invoice) — don't make people type it twice.
        if (newIsHistoric && string.IsNullOrWhiteSpace(newAmount) && !string.IsNullOrWhiteSpace(newPaidAmount))
            newAmount = newPaidAmount;
        if (!TryParseAmount(newAmount, out var amount)) { error = "Enter an invoice amount greater than zero in Amount £."; return; }
        decimal? paidAmount = null;
        if (newIsHistoric && !string.IsNullOrWhiteSpace(newPaidAmount))
        {
            // 0 = issued but never paid; blank = fully paid.
            if (!decimal.TryParse(newPaidAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var paid) || paid < 0)
            { error = "Enter a paid amount (0 = unpaid), or leave it blank for fully paid."; return; }
            if (paid > amount) { error = "Paid amount can't exceed the invoice amount."; return; }
            paidAmount = paid;
        }
        try
        {
            busy = true;
            if (newIsHistoric)
            {
                // Backdated to the period month server-side; counts toward certified immediately.
                await Invoices.CreateManualAsync(ProjectId, ParseMonth(newMonth), amount,
                    paidAmount ?? amount, issuedAt: null, paidAt: null,
                    note: string.IsNullOrWhiteSpace(newNote) ? null : newNote);
            }
            else
            {
                await Invoices.CreateAsync(ProjectId, ParseMonth(newMonth), amount, ValuationClaimId);
            }
            newAmount = "";
            newPaidAmount = "";
            newNote = "";
            await ReloadAsync();
            if (newIsHistoric) await OnCertifiedChanged.InvokeAsync();
        }
        catch { error = "Couldn't add the valuation invoice. Please try again."; }
        finally { busy = false; }
    }

    private async Task SubmitAsync(ValuationInvoice invoice)
    {
        await RunAsync(() => Invoices.SubmitAsync(invoice.ValuationInvoiceId),
            "Couldn't submit the invoice. Please try again.");
        // A snapshot was frozen — nudge the parent to refresh the report store so the
        // snapshot register shows it.
        if (error is null) await OnCertifiedChanged.InvokeAsync();
    }

    private async Task ApproveAsync(ValuationInvoice invoice)
    {
        await RunAsync(() => Invoices.ApproveAsync(invoice.ValuationInvoiceId),
            "Couldn't approve the invoice. Please try again.");
    }

    private void OpenReject(ValuationInvoice invoice)
    {
        rejectInvoice = invoice;
        rejectReason = "";
    }

    private async Task RejectAsync()
    {
        if (busy || rejectInvoice is null) return;
        if (string.IsNullOrWhiteSpace(rejectReason)) { error = "A rejection reason is required."; return; }
        var id = rejectInvoice.ValuationInvoiceId;
        var reason = rejectReason.Trim();
        rejectInvoice = null;
        await RunAsync(() => Invoices.RejectAsync(id, reason),
            "Couldn't record the rejection. Please try again.");
    }

    private async Task CancelAsync(ValuationInvoice invoice)
    {
        await RunAsync(() => Invoices.CancelAsync(invoice.ValuationInvoiceId),
            "Couldn't cancel the invoice. Please try again.");
        // Its snapshots were flagged superseded — refresh the register.
        if (error is null) await OnCertifiedChanged.InvokeAsync();
    }

    private void OpenEdit(ValuationInvoice invoice)
    {
        editInvoice = invoice;
        editMonth = invoice.PeriodMonth.ToString("yyyy-MM");
        editAmount = invoice.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        editPaidAmount = invoice.AmountPaid > 0 ? invoice.AmountPaid.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
        editNote = "";
    }

    private async Task SaveEditAsync()
    {
        if (busy || editInvoice is null) return;
        error = null;
        // Manual entries may be zeroed — correcting a mistaken historic figure without losing the
        // row. Workflow invoices still need a real amount.
        if (!TryParseAmount(editAmount, out var amount, allowZero: editInvoice.IsManual))
        {
            error = editInvoice.IsManual
                ? "Enter an amount of zero or more (0 voids the invoice's value)."
                : "Enter an amount greater than zero.";
            return;
        }
        decimal? paidAmount = null;
        if (amount == 0m)
        {
            // Zeroing the invoice zeroes its receipts with it — the paid total rolls back on save.
            paidAmount = 0m;
        }
        else if (editInvoice.IsManual && !string.IsNullOrWhiteSpace(editPaidAmount))
        {
            if (!decimal.TryParse(editPaidAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var paid) || paid < 0)
            { error = "Enter a valid paid amount, or leave it blank."; return; }
            if (paid > amount) { error = "Paid amount can't exceed the invoice amount."; return; }
            paidAmount = paid;
        }
        var invoice = editInvoice;
        var certifiedMoves = invoice.IsManual;
        editInvoice = null;
        try
        {
            busy = true;
            await Invoices.UpdateAsync(invoice.ValuationInvoiceId, ParseMonth(editMonth), amount,
                amountPaid: paidAmount,
                note: string.IsNullOrWhiteSpace(editNote) ? null : editNote);
            await ReloadAsync();
            if (certifiedMoves) await OnCertifiedChanged.InvokeAsync();
        }
        catch { error = "Couldn't save the invoice. Please try again."; }
        finally { busy = false; }
    }

    private async Task ToggleHistoryAsync(ValuationInvoice invoice)
    {
        if (historyOpenId == invoice.ValuationInvoiceId)
        {
            historyOpenId = null;
            historyEvents = null;
            return;
        }
        historyOpenId = invoice.ValuationInvoiceId;
        historyEvents = null;
        try { historyEvents = await Invoices.ListEventsAsync(invoice.ValuationInvoiceId); }
        catch { historyEvents = Array.Empty<ValuationInvoiceEvent>(); }
    }

    private async Task DeleteAsync(ValuationInvoice invoice)
    {
        if (busy) return;
        error = null;
        var countedTowardCertified = invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid;
        try
        {
            busy = true;
            await Invoices.DeleteAsync(invoice.ValuationInvoiceId);
            pendingDeleteId = null;
            await ReloadAsync();
            if (countedTowardCertified) await OnCertifiedChanged.InvokeAsync();
        }
        catch { error = "Couldn't delete the valuation invoice. Please try again."; }
        finally { busy = false; }
    }

    private async Task IssueAsync(ValuationInvoice invoice)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            await Invoices.IssueAsync(invoice.ValuationInvoiceId);
            await ReloadAsync();
            await OnCertifiedChanged.InvokeAsync();
        }
        catch { error = "Couldn't issue the invoice. Please try again."; }
        finally { busy = false; }
    }

    private void OpenPayment(ValuationInvoice invoice)
    {
        paymentInvoice = invoice;
        paymentAmount = invoice.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task RecordPaymentAsync()
    {
        if (busy || paymentInvoice is null) return;
        error = null;
        if (!TryParseAmount(paymentAmount, out var amount)) { error = "Enter an amount greater than zero."; return; }
        try
        {
            busy = true;
            await Invoices.RecordPaymentAsync(paymentInvoice.ValuationInvoiceId, amount);
            paymentInvoice = null;
            await ReloadAsync();
        }
        catch { error = "Couldn't record the payment. Please try again."; }
        finally { busy = false; }
    }

    // Shared wrapper for the one-click workflow actions (submit/approve/cancel).
    private async Task RunAsync(Func<Task<ValuationInvoice>> action, string failureMessage)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            await action();
            await ReloadAsync();
        }
        catch { error = failureMessage; }
        finally { busy = false; }
    }

    private static bool TryParseAmount(string value, out decimal amount, bool allowZero = false) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount)
        && (allowZero ? amount >= 0 : amount > 0);

    private static DateTimeOffset ParseMonth(string value) =>
        DateTimeOffset.TryParse(string.IsNullOrWhiteSpace(value) ? "" : value + "-01", out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

    private static string StatusLabel(ValuationInvoiceStatus status) => status switch
    {
        ValuationInvoiceStatus.Raised => "Draft",
        ValuationInvoiceStatus.Submitted => "Awaiting approval",
        ValuationInvoiceStatus.Approved => "Approved",
        ValuationInvoiceStatus.Rejected => "Rejected",
        ValuationInvoiceStatus.Issued => "Issued",
        ValuationInvoiceStatus.Paid => "Paid",
        ValuationInvoiceStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static string StatusTitle(ValuationInvoice invoice) => invoice.Status switch
    {
        ValuationInvoiceStatus.Raised => "Draft — send the claim for approval, or issue directly",
        ValuationInvoiceStatus.Submitted => $"With the client for approval{(invoice.SubmittedAt is { } s ? $" since {s:dd MMM yyyy}" : "")}",
        ValuationInvoiceStatus.Approved => "Approved by the client — issue to count toward certified to date",
        ValuationInvoiceStatus.Rejected => "Rejected — amend and resubmit, or cancel",
        ValuationInvoiceStatus.Issued => "Issued to the client — counts toward certified to date",
        ValuationInvoiceStatus.Paid => "Paid — rolled into the project's paid total",
        ValuationInvoiceStatus.Cancelled => "Withdrawn — excluded from every total",
        _ => ""
    };

    private static string BadgeClass(ValuationInvoiceStatus status)
    {
        const string baseClass = "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium ";
        return status switch
        {
            ValuationInvoiceStatus.Paid => baseClass + "bg-accent/10 border-accent/30 text-accent",
            ValuationInvoiceStatus.Issued => baseClass + "bg-info/10 border-info/30 text-info",
            ValuationInvoiceStatus.Submitted => baseClass + "bg-warning/10 border-warning/30 text-warning",
            ValuationInvoiceStatus.Approved => baseClass + "bg-positive/10 border-positive/30 text-positive",
            ValuationInvoiceStatus.Rejected => baseClass + "bg-negative/10 border-negative/30 text-negative",
            ValuationInvoiceStatus.Cancelled => baseClass + "bg-surface-raised border-line text-content-subtle line-through",
            _ => baseClass + "bg-surface-raised border-line text-content-muted"
        };
    }

    private static string EventLabel(ValuationInvoiceEventType type) => type switch
    {
        ValuationInvoiceEventType.Created => "Created",
        ValuationInvoiceEventType.Submitted => "Submitted for approval",
        ValuationInvoiceEventType.Approved => "Approved",
        ValuationInvoiceEventType.Rejected => "Rejected",
        ValuationInvoiceEventType.Amended => "Amended",
        ValuationInvoiceEventType.Issued => "Issued",
        ValuationInvoiceEventType.PaymentRecorded => "Payment recorded",
        ValuationInvoiceEventType.Cancelled => "Cancelled",
        ValuationInvoiceEventType.ManualEntry => "Historic entry",
        _ => type.ToString()
    };


    // Same rows and order as the table (cancelled invoices stay listed, greyed out).
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        if (invoices.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Valuation invoices",
            new ExcelColumn("Ref"),
            new ExcelColumn("Period", ExcelFormat.Date),
            new ExcelColumn("Amount £", ExcelFormat.Currency),
            new ExcelColumn("Status"),
            new ExcelColumn("Paid £", ExcelFormat.Currency));

        foreach (var invoice in invoices)
        {
            sheet.AddRow(
                invoice.DisplayNumber,
                invoice.PeriodMonth.LocalDateTime,
                invoice.Amount,
                StatusLabel(invoice.Status),
                invoice.AmountPaid);
        }
        return workbook;
    }
}
