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
