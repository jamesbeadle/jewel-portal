using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;


namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{
    [Parameter] public string ProjectId { get; set; } = "";

    // Session checked and the user is signed in. This is NOT "the orders are here": the heading,
    // the grouping toggle and the search box show at once, the table waits behind its own gate.

    // The remembered grouping decides how the whole table is rolled up, so the rows wait for it
    // rather than being grouped one way and re-grouped a moment later.
    private bool groupingReady;

    // A failed fetch must open the gate, or the jewel pulses forever.
    private bool dataFailed;

    // The panel reads all four: the orders, the cost-centre master its rows are named from, the
    // per-order invoiced balances behind "left to invoice", and the grouping choice.
    private bool OrdersReady =>
        WorkOrders.LoadedFor(ProjectId)
        && CostCenters.IsLoaded
        && summariesByOrder is not null
        && groupingReady;

    // Expanded group rows — cost codes in the cost-centre view, supplier names in the
    // supplier view; cleared when the grouping switches so the keys never mix.
    private readonly HashSet<string> expandedKeys = new(StringComparer.OrdinalIgnoreCase);

    private const string UnassignedCode = "(unassigned)";

    // PaidKnown: at least one order behind this row has a payment position JPMS can stand
    // behind (a linked bill, a migrated opening balance, or a net-credit order on which no
    // bill is ever due, so £0.00 paid is a fact). When no order in the row does,
    // the row's Paid is not zero — it is unknown — and the table says so with an em dash.
    // KnownCommitted is the committed value on just those known orders: Remaining is
    // KnownCommitted less Paid, so an unlinked order's value is never passed off as
    // wholly unpaid — and the Remaining column always sums to its footer total.
    private sealed record GroupRow(string Key, string Code, string Name, bool InMaster, int OrderCount, decimal Committed, decimal KnownCommitted, decimal Paid, decimal Invoiced, bool PaidKnown);
    private sealed record OrderLine(ProjectWorkOrderDetail Detail, WorkOrderLine Line);

    private IReadOnlyList<ProjectWorkOrderDetail> Orders => WorkOrders.Current(ProjectId);

    // The table, its totals and the export are the truth about what has been ISSUED and still
    // stands, so drafts, rejected drafts and cancelled orders stay out of all of them and live
    // in their own sections.
    private List<ProjectWorkOrderDetail> LiveOrders =>
        Orders.Where(detail => detail.Order.Status is not (WorkOrderStatus.Draft or WorkOrderStatus.Rejected or WorkOrderStatus.Cancelled)).ToList();

    private List<ProjectWorkOrderDetail> DraftOrders =>
        Orders.Where(detail => detail.Order.IsDraft && MatchesSearch(detail)).ToList();

    private List<ProjectWorkOrderDetail> RejectedOrders =>
        Orders.Where(detail => detail.Order.IsRejected && MatchesSearch(detail)).ToList();

    private List<ProjectWorkOrderDetail> CancelledOrders =>
        Orders.Where(detail => detail.Order.IsCancelled && MatchesSearch(detail)).ToList();

    private List<OrderLine> AllLines =>
        LiveOrders.SelectMany(detail => detail.Lines.Select(line => new OrderLine(detail, line))).ToList();

    // Grouping toggle: by cost centre (the QS view, default) or by supplier (how the
    // accountant records costs). The choice is remembered per user, per browser.
    private bool groupBySupplier;

    private int GroupColspan => groupBySupplier ? 6 : 7;

    private async Task SetGroupingAsync(bool bySupplier)
    {
        if (groupBySupplier == bySupplier) return;
        groupBySupplier = bySupplier;
        expandedKeys.Clear();
        if (Auth.CurrentUser is not null)
        {
            await GroupingStorage.WriteAsync(Auth.CurrentUser.Email, bySupplier);
        }
    }

    // Supplier search: typing narrows the table (and the no-breakdown list) to orders
    // whose supplier matches, auto-expanding the groups that remain.
    private string supplierSearch = "";

    private bool Searching => !string.IsNullOrWhiteSpace(supplierSearch);

    private bool MatchesSearch(ProjectWorkOrderDetail detail) =>
        !Searching || detail.SubcontractorName.Contains(supplierSearch.Trim(), StringComparison.OrdinalIgnoreCase);

    private List<OrderLine> VisibleLines =>
        AllLines.Where(entry => MatchesSearch(entry.Detail)).ToList();

    private int VisibleOrderCount =>
        LiveOrders.Count(detail => MatchesSearch(detail));

    private List<ProjectWorkOrderDetail> OrdersWithoutLines =>
        LiveOrders.Where(detail => detail.Lines.Count == 0 && MatchesSearch(detail)).ToList();

    private decimal TotalCommitted => LiveOrders.Sum(detail => detail.Order.Value);
    private decimal TotalPaid => AllLines.Sum(entry => entry.Line.PaidToDate);
    private decimal TotalInvoiced => SummariesByOrder.Values.Sum(summary => summary.InvoicedToDate);

    // Per-order invoiced balances from the linked Xero purchase lines — the same figures
    // the WO Allocation tab manages. Fetched once per tab entry alongside the orders.
    // Nullable on purpose: no summaries is a real answer that means nothing has been invoiced,
    // and it is indistinguishable from a fetch that hasn't landed.
    private Dictionary<string, WorkOrderInvoiceSummary>? summariesByOrder;

    private static readonly Dictionary<string, WorkOrderInvoiceSummary> NoSummaries = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, WorkOrderInvoiceSummary> SummariesByOrder => summariesByOrder ?? NoSummaries;

    // An em dash, not a zero. "GBP 0.00 paid" and "we have not been told what is paid" are
    // different answers, and the second one must not be able to pass for the first.
    private const string Dash = "–";

    private const string UnknownPaidHint =
        "No Xero bill is linked to this order yet, so nothing is known about what has been paid — link its bills on the WO Allocation tab";

    private WorkOrderPaymentStatus PaymentStatusOf(ProjectWorkOrderDetail detail) =>
        SummariesByOrder.TryGetValue(detail.Order.WorkOrderId, out var summary)
            ? summary.PaymentStatus
            : WorkOrderPaymentStatus.NotLinked;

    private bool PaymentKnown(ProjectWorkOrderDetail detail) =>
        PaymentStatusOf(detail) != WorkOrderPaymentStatus.NotLinked;

    // Table footer totals — over the visible (search-filtered) lines, so a supplier
    // search reads as that supplier's committed / paid position.
    private decimal LineCommitted => VisibleLines.Sum(entry => entry.Line.LineTotal);
    private decimal LinePaid => VisibleLines.Sum(entry => entry.Line.PaidToDate);
    private decimal LineInvoiced => VisibleLines.Sum(entry => InvoicedShare(entry));

    // Remaining is only an answer where the payment position is known. An unlinked order
    // carries PaidToDate = 0 — not because £0 has been paid but because nothing is known —
    // so committed-less-paid over ALL lines would quietly count its full value as remaining
    // while its row shows “–”: the total wouldn't match the column above it (which is
    // exactly how this was found). Remaining therefore sums known orders only, and the
    // committed value still unknown is said out loud next to the figures it is missing from.
    private decimal KnownRemainingOf(List<OrderLine> lines) =>
        lines.Where(entry => PaymentKnown(entry.Detail))
             .Sum(entry => entry.Line.LineTotal - entry.Line.PaidToDate);

    private decimal UnknownCommittedOf(List<OrderLine> lines) =>
        lines.Where(entry => !PaymentKnown(entry.Detail)).Sum(entry => entry.Line.LineTotal);

    private const string UnknownCommittedHint =
        "Committed value on orders with no Xero bill linked — nothing is known about what has " +
        "been paid on them, so they count in neither paid nor remaining. Link their bills on " +
        "the WO Allocation tab and they move into the remaining figure.";

    private List<GroupRow> Rows => RowsFrom(VisibleLines);

    // The export can be asked to ignore the supplier search — the same grouping pipeline
    // runs over either the search-narrowed lines (the table's view) or every line.
    private List<GroupRow> RowsFrom(List<OrderLine> lines) =>
        groupBySupplier ? SupplierRowsFrom(lines) : CostCentreRowsFrom(lines);

    // Master cost centres in master order first, then codes not in the active master (legacy /
    // retired), then lines with no code at all — shown rather than silently swallowed.
    private List<GroupRow> CostCentreRowsFrom(List<OrderLine> lines)
    {
        var masterOrder = CostCenters.Current
            .Select((centre, index) => (centre.Code, index))
            .ToDictionary(entry => entry.Code, entry => entry.index, StringComparer.OrdinalIgnoreCase);
        var namesByCode = CostCenters.Current.ToDictionary(centre => centre.Code, centre => centre.Name, StringComparer.OrdinalIgnoreCase);

        return lines
            .GroupBy(entry => CodeOf(entry), StringComparer.OrdinalIgnoreCase)
            .Select(group => new GroupRow(
                group.Key,
                group.Key,
                namesByCode.TryGetValue(group.Key, out var name) ? name : "",
                namesByCode.ContainsKey(group.Key),
                group.Select(entry => entry.Detail.Order.WorkOrderId).Distinct().Count(),
                group.Sum(entry => entry.Line.LineTotal),
                group.Where(entry => PaymentKnown(entry.Detail)).Sum(entry => entry.Line.LineTotal),
                group.Sum(entry => entry.Line.PaidToDate),
                group.Sum(entry => InvoicedShare(entry)),
                group.Any(entry => PaymentKnown(entry.Detail))))
            .OrderBy(row => row.Code == UnassignedCode ? 2 : row.InMaster ? 0 : 1)
            .ThenBy(row => masterOrder.TryGetValue(row.Code, out var index) ? index : int.MaxValue)
            .ThenBy(row => row.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Supplier roll-up: the same lines and totals, grouped by who the order is with
    // rather than where the cost sits — how the accountant records them.
    private List<GroupRow> SupplierRowsFrom(List<OrderLine> lines) =>
        lines
            .GroupBy(entry => entry.Detail.SubcontractorName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GroupRow(
                group.Key,
                "",
                group.Key,
                true,
                group.Select(entry => entry.Detail.Order.WorkOrderId).Distinct().Count(),
                group.Sum(entry => entry.Line.LineTotal),
                group.Where(entry => PaymentKnown(entry.Detail)).Sum(entry => entry.Line.LineTotal),
                group.Sum(entry => entry.Line.PaidToDate),
                group.Sum(entry => InvoicedShare(entry)),
                group.Any(entry => PaymentKnown(entry.Detail))))
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string CodeOf(OrderLine entry) =>
        string.IsNullOrWhiteSpace(entry.Line.CostCode) ? UnassignedCode : entry.Line.CostCode;

    // "Left to invoice" is a whole-order figure (its value less what's been invoiced), but
    // this table rolls up by cost centre / supplier and one order can span several centres.
    // So we spread each order's invoiced-to-date across its own lines in proportion to line
    // value, giving each line its share. Summed per group this never double-counts, and
    // summed over a whole order it collapses back to the order's invoiced total — so the
    // footer's committed-less-invoiced matches the "left to invoice" figure in the summary
    // line above and on the WO Allocation tab.
    private decimal InvoicedShare(OrderLine entry)
    {
        if (!SummariesByOrder.TryGetValue(entry.Detail.Order.WorkOrderId, out var summary))
            return 0m;
        var orderLineTotal = entry.Detail.Lines.Sum(line => line.LineTotal);
        return orderLineTotal == 0m
            ? 0m
            : summary.InvoicedToDate * (entry.Line.LineTotal / orderLineTotal);
    }

    private string CostCentreNameFor(string code) =>
        CostCenters.Current.FirstOrDefault(centre =>
            string.Equals(centre.Code, code, StringComparison.OrdinalIgnoreCase))?.Name ?? "";

    private List<OrderLine> LinesFor(string key) => LinesFrom(VisibleLines, key);

    private List<OrderLine> LinesFrom(List<OrderLine> lines, string key) =>
        lines
            .Where(entry => groupBySupplier
                ? string.Equals(entry.Detail.SubcontractorName, key, StringComparison.OrdinalIgnoreCase)
                : string.Equals(CodeOf(entry), key, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Detail.Order.Number)
            .ThenBy(entry => entry.Line.SortOrder)
            .ToList();

    // While a supplier search is active every remaining group stays open — the point of
    // the search is to see the matching orders, not to click each group open.
    private bool IsExpanded(string key) => Searching || expandedKeys.Contains(key);

    // The line being re-coded across cost centres, if the modal is open.
    private OrderLine? recoding;

    private void OpenRecode(OrderLine line) => recoding = line;

    private void CloseRecode() => recoding = null;

    private async Task HandleRecodedAsync()
    {
        recoding = null;
        await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
    }

    // The manual "Add work order" flow, which can also package the fresh order
    // against the valuation lines that priced its scope. The same modal edits a
    // manually raised order when editingOrder is set.
    private bool manualOrderOpen;
    private ProjectWorkOrderDetail? editingOrder;

    private void OpenAdd()
    {
        editingOrder = null;
        manualOrderOpen = true;
    }

    private void OpenEdit(ProjectWorkOrderDetail detail)
    {
        editingOrder = detail;
        manualOrderOpen = true;
    }

    private void CloseManualOrder()
    {
        manualOrderOpen = false;
        editingOrder = null;
    }

    private async Task HandleManualOrderSavedAsync()
    {
        manualOrderOpen = false;
        editingOrder = null;
        await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
    }

    private void Toggle(string key)
    {
        if (!expandedKeys.Remove(key)) expandedKeys.Add(key);
    }

    private static string Reference(WorkOrder order) => order.Reference;

    // ── Deciding a draft: approve (mints the next number) or reject (terminal) ──
    // Two clicks on purpose either way: an approval mints a number that is never taken
    // back, and a rejection has no undo. pendingDraftId holds the draft awaiting its
    // confirm click, pendingDecision which decision was asked for.
    private enum DraftDecision { Approve, Reject }

    // Drafts link to the same printable purchase order page as issued orders — it renders
    // unreleased orders with a Draft reference and badge, so the sheet can be read (and saved
    // as a PDF) BEFORE approving releases the order and emails the PO to the supplier.
    private string PurchaseOrderPathFor(ProjectWorkOrderDetail detail) =>
        $"/projects/{ProjectId}/work-orders/{detail.Order.WorkOrderId}/po";

    private string? pendingDraftId;
    private DraftDecision pendingDecision;
    private bool decisionBusy;
    private string? decisionError;

    // The delete confirm modal: which order it is asking about — an undecided draft or a
    // rejected one — and its own busy/error state so a refused delete reads back inside the
    // modal rather than under the list.
    private ProjectWorkOrderDetail? deletingOrder;
    private bool deleteBusy;
    private string? deleteError;

    private bool DeletingRejectedOrder => deletingOrder?.Order.IsRejected == true;

    private DraftDecision? PendingFor(ProjectWorkOrderDetail detail) =>
        pendingDraftId == detail.Order.WorkOrderId ? pendingDecision : null;

    // ── Cancelling an issued order: terminal, directors and the FD only ──
    // Same two-click shape as the draft decisions: cancelling voids an order the supplier
    // has already been sent, and there is no undo. The API is the real gate (CancelWorkOrder
    // is Admin/Director/FD only); CanCancelOrders just keeps the action out of everyone
    // else's way. Admin expands to every role at sign-in, so checking the two roles is enough.
    private bool CanCancelOrders =>
        Auth.CurrentRoles.Contains(Role.ManagingDirector) || Auth.CurrentRoles.Contains(Role.FinanceDirector);

    // ── Editing an order: manual orders as before; ANY order for the directors ──
    // The accountant's flow (2026-08-21): open WO-0045, add the £80 line from the email, save,
    // download the updated PO and send it back by hand. The API is the real gate (the endpoint
    // stamps the director flag onto UpdateManualWorkOrder); this just keeps Edit out of everyone
    // else's menus. Admin expands to every role at sign-in, so checking the two roles is enough.
    private bool CanEditAllOrders =>
        Auth.CurrentRoles.Contains(Role.ManagingDirector) || Auth.CurrentRoles.Contains(Role.FinanceDirector);

    private bool CanEditOrder(WorkOrder order) => order.IsManual || CanEditAllOrders;

    // The table row whose Actions menu is open — while set, the table container's overflow cap
    // is lifted so the menu isn't clipped (see the container's comment).
    private string? openRowMenuKey;

    private IReadOnlyList<DropdownMenu.Item> LineMenuItems(OrderLine line)
    {
        var items = new List<DropdownMenu.Item>
        {
            new("View PO",
                Href: $"/projects/{ProjectId}/work-orders/{line.Detail.Order.WorkOrderId}/po",
                Hint: "View / print the purchase order sent to the supplier")
        };
        if (CanEditOrder(line.Detail.Order))
        {
            items.Add(new DropdownMenu.Item("Edit…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenEdit(line.Detail)),
                Hint: line.Detail.Order.IsManual
                    ? "This order was raised manually — edit its supplier, title, scope and priced lines"
                    : "Correct this order's supplier, title, scope and priced lines — directors only; the updated PO is downloaded and sent by hand"));
        }
        items.Add(new DropdownMenu.Item("Re-code this line…",
            OnSelect: EventCallback.Factory.Create(this, () => OpenRecode(line)),
            Hint: "Move this line to another cost centre, or split its amount across several — the order's value never changes",
            Group: 1));
        if (CanCancelOrders)
        {
            items.Add(new DropdownMenu.Item("Cancel order…",
                OnSelect: EventCallback.Factory.Create(this, () => SetCancelPending(line.Detail)),
                Hint: "Cancel this issued order — void the whole order (not just this line); it keeps its number as a record but stops counting everywhere. Refused while bills are linked or money is paid against it.",
                Destructive: true,
                Group: 2));
        }
        return items;
    }

    private IReadOnlyList<DropdownMenu.Item> OrderMenuItems(ProjectWorkOrderDetail detail)
    {
        var items = new List<DropdownMenu.Item>
        {
            new("View PO",
                Href: $"/projects/{ProjectId}/work-orders/{detail.Order.WorkOrderId}/po",
                Hint: "View / print the purchase order sent to the supplier")
        };
        if (CanEditOrder(detail.Order))
        {
            items.Add(new DropdownMenu.Item("Edit…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenEdit(detail)),
                Hint: detail.Order.IsManual
                    ? "This order was raised manually — edit its supplier, title, scope and priced lines"
                    : "Correct this order's supplier, title, scope and priced lines — directors only; the updated PO is downloaded and sent by hand"));
        }
        if (CanCancelOrders)
        {
            items.Add(new DropdownMenu.Item("Cancel order…",
                OnSelect: EventCallback.Factory.Create(this, () => SetCancelPending(detail)),
                Hint: "Cancel this issued order — void it; it keeps its number as a record but stops counting everywhere. Refused while bills are linked or money is paid against it.",
                Destructive: true,
                Group: 1));
        }
        return items;
    }



    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        CostCenters.OnChanged += StateHasChanged;
        WorkOrders.OnChanged += StateHasChanged;
        // Refresh once per tab entry (stale-while-revalidate, per the front-end
        // data-loading convention) — cached figures render immediately, then update.
        _ = WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
        // The supplier directory (approve-warning email addresses) and project name (the
        // auto-sent PO email's subject) follow the same convention.
        _ = Subcontractors.RefreshAsync(CancellationToken.None);
        _ = Projects.RefreshAsync(CancellationToken.None);

        // The saved grouping, the cost centres and the invoice summaries are independent of each
        // other, so they go out together instead of in series.
        var groupingTask = GroupingStorage.ReadGroupBySupplierAsync(Auth.CurrentUser!.Email);
        var summariesTask = Queries.AskAsync(new ListWorkOrderInvoiceSummaries(ProjectId), CancellationToken.None);
        try
        {
            await Task.WhenAll(CostCenters.RefreshAsync(CancellationToken.None), groupingTask, summariesTask);

            // Reopen in whichever grouping this user last used (cost centre by default).
            groupBySupplier = await groupingTask;
            groupingReady = true;
            summariesByOrder = (await summariesTask).ToDictionary(summary => summary.WorkOrderId, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // HttpQueryClient has already reported this to the error toast with a reference; here
            // we only need to stop the table waiting on data that is not coming.
            dataFailed = true;
        }

    }

    public void Dispose()
    {
        CostCenters.OnChanged -= StateHasChanged;
        WorkOrders.OnChanged -= StateHasChanged;
    }
}
