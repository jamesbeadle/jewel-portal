using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using static Jewel.JPMS.Features.Procurement.WorkOrderDisplay;
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

    private List<WorkOrderLineEntry> AllLines =>
        LiveOrders.SelectMany(detail => detail.Lines.Select(line => new WorkOrderLineEntry(detail, line))).ToList();

    // Grouping toggle: by cost centre (the QS view, default) or by supplier (how the
    // accountant records costs). The choice is remembered per user, per browser.
    private bool groupBySupplier;

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

    private List<WorkOrderLineEntry> VisibleLines =>
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

    // Project-wide, so any row carries it; null until a purchase line has ever been synced.
    private DateTimeOffset? LedgerSyncedAtUtc =>
        SummariesByOrder.Values.Select(summary => summary.LedgerSyncedAtUtc).FirstOrDefault();

    private WorkOrderPaymentStatus PaymentStatusOf(ProjectWorkOrderDetail detail) =>
        SummariesByOrder.TryGetValue(detail.Order.WorkOrderId, out var summary)
            ? summary.PaymentStatus
            : WorkOrderPaymentStatus.NotLinked;

    private bool PaymentKnown(ProjectWorkOrderDetail detail) =>
        PaymentStatusOf(detail) != WorkOrderPaymentStatus.NotLinked;

    // The table's footer — over the visible (search-filtered) lines, so a supplier search
    // reads as that supplier's committed / paid position — in the same shape as a row.
    private WorkOrderGroup VisibleTotal => TotalOf(VisibleLines, VisibleOrderCount);

    private WorkOrderGroup TotalOf(List<WorkOrderLineEntry> lines, int orderCount) =>
        new("", "", "", true, orderCount,
            lines.Sum(entry => entry.Line.LineTotal),
            lines.Where(entry => PaymentKnown(entry.Detail)).Sum(entry => entry.Line.LineTotal),
            lines.Sum(entry => entry.Line.PaidToDate),
            lines.Sum(entry => InvoicedShare(entry)),
            true);

    // Remaining is only an answer where the payment position is known. An unlinked order
    // carries PaidToDate = 0 — not because £0 has been paid but because nothing is known —
    // so committed-less-paid over ALL lines would quietly count its full value as remaining
    // while its row shows “–”: the total wouldn't match the column above it (which is
    // exactly how this was found). Remaining therefore sums known orders only, and the
    // committed value still unknown is said out loud next to the figures it is missing from.
    private decimal KnownRemainingOf(List<WorkOrderLineEntry> lines) =>
        lines.Where(entry => PaymentKnown(entry.Detail))
             .Sum(entry => entry.Line.LineTotal - entry.Line.PaidToDate);

    private decimal UnknownCommittedOf(List<WorkOrderLineEntry> lines) =>
        lines.Where(entry => !PaymentKnown(entry.Detail)).Sum(entry => entry.Line.LineTotal);

    private List<WorkOrderGroup> Rows => RowsFrom(VisibleLines);

    // The export can be asked to ignore the supplier search — the same grouping pipeline
    // runs over either the search-narrowed lines (the table's view) or every line.
    private List<WorkOrderGroup> RowsFrom(List<WorkOrderLineEntry> lines) =>
        groupBySupplier ? SupplierRowsFrom(lines) : CostCentreRowsFrom(lines);

    // Master cost centres in master order first, then codes not in the active master (legacy /
    // retired), then lines with no code at all — shown rather than silently swallowed.
    private List<WorkOrderGroup> CostCentreRowsFrom(List<WorkOrderLineEntry> lines)
    {
        var masterOrder = CostCenters.Current
            .Select((centre, index) => (centre.Code, index))
            .ToDictionary(entry => entry.Code, entry => entry.index, StringComparer.OrdinalIgnoreCase);
        var namesByCode = CostCenters.Current.ToDictionary(centre => centre.Code, centre => centre.Name, StringComparer.OrdinalIgnoreCase);

        return lines
            .GroupBy(entry => CodeOf(entry), StringComparer.OrdinalIgnoreCase)
            .Select(group => new WorkOrderGroup(
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
    private List<WorkOrderGroup> SupplierRowsFrom(List<WorkOrderLineEntry> lines) =>
        lines
            .GroupBy(entry => entry.Detail.SubcontractorName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new WorkOrderGroup(
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

    private static string CodeOf(WorkOrderLineEntry entry) =>
        string.IsNullOrWhiteSpace(entry.Line.CostCode) ? UnassignedCode : entry.Line.CostCode;

    // "Left to invoice" is a whole-order figure (its value less what's been invoiced), but
    // this table rolls up by cost centre / supplier and one order can span several centres.
    // So we spread each order's invoiced-to-date across its own lines in proportion to line
    // value, giving each line its share. Summed per group this never double-counts, and
    // summed over a whole order it collapses back to the order's invoiced total — so the
    // footer's committed-less-invoiced matches the "left to invoice" figure in the summary
    // line above and on the WO Allocation tab.
    private decimal InvoicedShare(WorkOrderLineEntry entry)
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

    private IReadOnlyList<WorkOrderLineEntry> LinesFor(string key) => LinesFrom(VisibleLines, key);

    private List<WorkOrderLineEntry> LinesFrom(List<WorkOrderLineEntry> lines, string key) =>
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
    private WorkOrderLineEntry? recoding;

    private void OpenRecode(WorkOrderLineEntry line) => recoding = line;

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

    private DeleteWorkOrderModal deleteModal = default!;
    // A delete is in flight — the rejected list's menus stand down on it.
    private bool deleteBusy;

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

    private IReadOnlyList<DropdownMenu.Item> LineMenuItems(WorkOrderLineEntry line)
    {
        var items = new List<DropdownMenu.Item>
        {
            new("View PO",
                Href: PurchaseOrderPath(ProjectId, line.Detail.Order.WorkOrderId),
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
                Href: PurchaseOrderPath(ProjectId, detail.Order.WorkOrderId),
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
