using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrderAllocation
{
    [Parameter] public string ProjectId { get; set; } = "";

    private enum QueueFilter { Unlinked, Linked, All }

    private static readonly (QueueFilter Filter, string Label)[] FilterOptions =
    {
        (QueueFilter.Unlinked, "Unlinked"),
        (QueueFilter.Linked, "Linked"),
        (QueueFilter.All, "All")
    };

    private bool isLoaded;
    private List<WorkOrderInvoiceSummary> summaries = new();
    private List<ProjectCostOfSalesLine> AllLines = new();
    private QueueFilter queueFilter = QueueFilter.Unlinked;
    private string orderSearch = "";
    private readonly HashSet<string> expandedOrderIds = new(StringComparer.OrdinalIgnoreCase);
    // Lines whose link is being saved — only those rows' controls disable.
    private readonly HashSet<string> busyLineIds = new();
    private string? linkError;
    // Bumped when a save fails, recreating the selects so they fall back to the stored link.
    private int revertNonce;
    // The line whose amount split is open in the split editor, if any.
    private ProjectCostOfSalesLine? splitLine;

    private List<WorkOrderInvoiceSummary> FilteredSummaries =>
        string.IsNullOrWhiteSpace(orderSearch)
            ? summaries
            : summaries.Where(summary =>
                    (summary.SubcontractorName?.Contains(orderSearch.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)
                    || (summary.Title?.Contains(orderSearch.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

    // Anything not yet paying a work order — partially split lines count their remainder.
    private decimal UnlinkedTotal => AllLines.Sum(line => line.UnlinkedRemainder);

    // The one search box filters BOTH tables: matching a supplier at the top brings up the
    // same supplier's invoice lines below, so orders and unlinked bills line up side by side.
    private bool MatchesSearch(ProjectCostOfSalesLine line)
    {
        if (string.IsNullOrWhiteSpace(orderSearch)) return true;
        var term = orderSearch.Trim();
        return line.Supplier.Contains(term, StringComparison.OrdinalIgnoreCase)
               || line.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
               || line.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
               || line.CostCode.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    // Supplier names that have at least one work order on this project, matched exactly
    // (case-insensitive, trimmed). A line whose supplier is in here can actually be
    // allocated to an order; anything else has no matching supplier to link against.
    private HashSet<string> WorkOrderSupplierNames =>
        summaries
            .Where(summary => !string.IsNullOrWhiteSpace(summary.SubcontractorName))
            .Select(summary => summary.SubcontractorName!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private List<ProjectCostOfSalesLine> QueueLines
    {
        get
        {
            var workOrderSuppliers = WorkOrderSupplierNames;
            bool SupplierHasWorkOrder(ProjectCostOfSalesLine line) =>
                !string.IsNullOrWhiteSpace(line.Supplier)
                && workOrderSuppliers.Contains(line.Supplier.Trim());

            return (queueFilter switch
                {
                    QueueFilter.Unlinked => AllLines.Where(line => line.UnlinkedRemainder != 0m),
                    QueueFilter.Linked => AllLines.Where(line => line.Links.Count > 0),
                    _ => AllLines.AsEnumerable()
                })
                .Where(MatchesSearch)
                // Lines from suppliers that have a work order come first (the ones you can
                // match); suppliers with no matching work order fall below (the ones to
                // ignore). OrderBy is stable, so each group keeps its existing order.
                .OrderBy(line => SupplierHasWorkOrder(line) ? 0 : 1)
                .ToList();
        }
    }

    // The lines paying this order, each with its slice amount (a bill split across
    // several orders contributes only its slice here).
    private List<(ProjectCostOfSalesLine Line, decimal Slice)> LinesLinkedTo(string workOrderId) =>
        AllLines
            .GroupBy(line => line.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(line => (Line: line, Slice: line.Links.FirstOrDefault(link =>
                string.Equals(link.WorkOrderId, workOrderId, StringComparison.OrdinalIgnoreCase))?.Amount ?? 0m))
            .Where(linked => linked.Slice != 0m)
            .OrderBy(linked => linked.Line.Date ?? DateTime.MaxValue)
            .ToList();

    // A single full-amount slice renders as the plain dropdown; anything else (several
    // orders, or one partial slice) is an amount split the dropdown can't express.
    private static string? SingleLinkId(ProjectCostOfSalesLine line) =>
        line.Links.Count == 1 ? line.Links[0].WorkOrderId : null;

    private static bool IsAmountSplit(ProjectCostOfSalesLine line) =>
        line.Links.Count > 1 || (line.Links.Count == 1 && line.Links[0].Amount != line.Net);

    private string SplitTitle(ProjectCostOfSalesLine line) =>
        string.Join(" · ", line.Links.Select(link =>
        {
            var summary = summaries.FirstOrDefault(candidate =>
                string.Equals(candidate.WorkOrderId, link.WorkOrderId, StringComparison.OrdinalIgnoreCase));
            return summary is null ? MoneyExact(link.Amount) : $"WO-{summary.Number:0000} {MoneyExact(link.Amount)}";
        }))
        + (line.UnlinkedRemainder == 0m ? "" : $" · {MoneyExact(line.UnlinkedRemainder)} not linked");

    // The split editor offers every live order with its balance. Cancelled, draft and
    // rejected orders are omitted — the API refuses links to all three.
    private IReadOnlyList<WorkOrderInvoiceSummary> LiveSummaries =>
        summaries.Where(summary => summary.Status is not (WorkOrderStatus.Cancelled or WorkOrderStatus.Draft or WorkOrderStatus.Rejected)).ToList();

    // The dropdown offers live orders whose lines are coded to the invoice line's centre
    // first (the natural match), then everything else under a separate group. Cancelled,
    // draft and rejected orders are omitted — the API refuses links to all three.
    private (List<WorkOrderInvoiceSummary> Matching, List<WorkOrderInvoiceSummary> Others) OptionsFor(ProjectCostOfSalesLine line)
    {
        var codesByOrder = WorkOrders.Current(ProjectId)
            .ToDictionary(detail => detail.Order.WorkOrderId,
                detail => detail.Lines.Select(orderLine => orderLine.CostCode).ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        var live = summaries.Where(summary => summary.Status is not (WorkOrderStatus.Cancelled or WorkOrderStatus.Draft or WorkOrderStatus.Rejected)).ToList();
        var matching = live.Where(summary =>
                codesByOrder.TryGetValue(summary.WorkOrderId, out var codes) && codes.Contains(line.CostCode))
            .ToList();
        var others = live.Except(matching).ToList();
        return (matching, others);
    }


    private async Task LinkAsync(ProjectCostOfSalesLine line, string? workOrderId)
    {
        if (!busyLineIds.Add(line.XeroLedgerLineId)) return;
        var newLink = string.IsNullOrEmpty(workOrderId) ? null : workOrderId;
        if (newLink == SingleLinkId(line) && !IsAmountSplit(line))
        {
            busyLineIds.Remove(line.XeroLedgerLineId);
            return;
        }
        linkError = null;
        var slices = newLink is null
            ? (IReadOnlyList<XeroWorkOrderLinkSlice>)Array.Empty<XeroWorkOrderLinkSlice>()
            : new[] { new XeroWorkOrderLinkSlice(newLink, line.Net) }; // whole line to one order
        try
        {
            await Commands.SendAsync(new SetXeroLineWorkOrderLinks(ProjectId, line.XeroLedgerLineId, slices), CancellationToken.None);
            await RefreshAsync();
        }
        catch (CommandFailedException ex)
        {
            linkError = $"Couldn't link this line: {ex.Message}";
            revertNonce++; // snap the dropdown back to the stored link — the save didn't happen
        }
        finally
        {
            busyLineIds.Remove(line.XeroLedgerLineId);
        }
    }

    // Removes one order's slice from a split, leaving the line's other slices in place.
    private async Task UnlinkSliceAsync(ProjectCostOfSalesLine line, string workOrderId)
    {
        if (!busyLineIds.Add(line.XeroLedgerLineId)) return;
        linkError = null;
        var remaining = line.Links
            .Where(link => !string.Equals(link.WorkOrderId, workOrderId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        try
        {
            await Commands.SendAsync(new SetXeroLineWorkOrderLinks(ProjectId, line.XeroLedgerLineId, remaining), CancellationToken.None);
            await RefreshAsync();
        }
        catch (CommandFailedException ex)
        {
            linkError = $"Couldn't unlink this line: {ex.Message}";
        }
        finally
        {
            busyLineIds.Remove(line.XeroLedgerLineId);
        }
    }

    private async Task HandleSplitSavedAsync(IReadOnlyList<XeroWorkOrderLinkSlice> slices)
    {
        splitLine = null;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var summariesTask = Queries.AskAsync(new ListWorkOrderInvoiceSummaries(ProjectId), CancellationToken.None);
        var linesTask = Queries.AskAsync(new ListProjectCostOfSalesLines(ProjectId), CancellationToken.None);
        // Drafts and rejected drafts are dropped at the door: unnumbered, uninvoiceable,
        // nothing to allocate against — either would inflate "left to invoice" with an
        // order that hasn't been (or never will be) issued. Cancelled orders too: the API
        // refuses new links to them, and CancelWorkOrder refuses while anything is linked,
        // so a cancelled order here could only ever show its full value as "left to
        // invoice" on a commitment that no longer stands.
        summaries = (await summariesTask)
            .Where(summary => summary.Status is not (WorkOrderStatus.Draft or WorkOrderStatus.Rejected or WorkOrderStatus.Cancelled))
            .ToList();
        AllLines = (await linesTask).ToList();
    }

    private void ToggleOrder(string workOrderId)
    {
        if (!expandedOrderIds.Remove(workOrderId)) expandedOrderIds.Add(workOrderId);
    }


    private string ProgressTitle(WorkOrderInvoiceSummary summary) =>
        $"{MoneyExact(summary.InvoicedToDate)} invoiced of {MoneyExact(summary.Value)} · {MoneyExact(summary.RemainingToInvoice)} left · {summary.LinkedLineCount} line{(summary.LinkedLineCount == 1 ? "" : "s")}";

    private static int ProgressPercent(WorkOrderInvoiceSummary summary) =>
        summary.Value <= 0m ? (summary.InvoicedToDate > 0m ? 100 : 0)
            : (int)Math.Clamp(Math.Round(summary.InvoicedToDate / summary.Value * 100m), 0, 100);


    private static string MoneyExact(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

    private static string InvoicingLabel(WorkOrderInvoicingStatus status) => status switch
    {
        WorkOrderInvoicingStatus.NotInvoiced => "Not invoiced",
        WorkOrderInvoicingStatus.PartInvoiced => "Part invoiced",
        WorkOrderInvoicingStatus.FullyInvoiced => "Fully invoiced",
        _ => "Over invoiced"
    };

    // The queue's Work order column reuses the page's own split/link helpers so the
    // text matches what the dropdown/split summary shows: "—" for a cost-centre split
    // (can't be linked at all), the split summary for an amount split, otherwise the
    // single linked order's reference (or "Not linked").
    private string? WorkOrderText(ProjectCostOfSalesLine line)
    {
        if (line.IsSplit) return null;
        if (IsAmountSplit(line)) return SplitTitle(line);
        var linkId = SingleLinkId(line);
        if (linkId is null) return "Not linked";
        var summary = summaries.FirstOrDefault(candidate =>
            string.Equals(candidate.WorkOrderId, linkId, StringComparison.OrdinalIgnoreCase));
        return summary is null ? linkId : $"WO-{summary.Number:0000}";
    }

    // One workbook, two sheets: the work-order table (respecting the supplier/title
    // search) and the invoice-line queue (respecting that same search plus the
    // Unlinked/Linked/All chip) — both with their tfoot total row. "Include all lines"
    // (offered while the chip or search narrows the tables — note the chip defaults to
    // Unlinked) exports every work order and every invoice line instead.
    private ExcelWorkbook? BuildExportWorkbook(bool includeAllLines)
    {
        if (summaries.Count == 0 && AllLines.Count == 0) return null;

        var workbook = new ExcelWorkbook();

        var ordersSheet = workbook.AddSheet("Work orders",
            new ExcelColumn("Order"),
            new ExcelColumn("Supplier"),
            new ExcelColumn("Title"),
            new ExcelColumn("Invoicing"),
            new ExcelColumn("Value", ExcelFormat.Currency),
            new ExcelColumn("Invoiced", ExcelFormat.Currency),
            new ExcelColumn("Left to invoice", ExcelFormat.Currency));
        var filteredSummaries = includeAllLines ? summaries : FilteredSummaries;
        foreach (var summary in filteredSummaries)
        {
            ordersSheet.AddRow(
                $"WO-{summary.Number:0000}",
                summary.SubcontractorName,
                summary.Title,
                InvoicingLabel(summary.InvoicingStatus),
                summary.Value,
                summary.InvoicedToDate,
                summary.RemainingToInvoice);
        }
        ordersSheet.AddRow(
            includeAllLines ? "Total" : "Total shown", null, null, null,
            filteredSummaries.Sum(summary => summary.Value),
            filteredSummaries.Sum(summary => summary.InvoicedToDate),
            filteredSummaries.Sum(summary => summary.RemainingToInvoice));

        var linesSheet = workbook.AddSheet("Invoice lines",
            new ExcelColumn("Date", ExcelFormat.Date),
            new ExcelColumn("Supplier"),
            new ExcelColumn("Invoice"),
            new ExcelColumn("Description"),
            new ExcelColumn("Centre"),
            new ExcelColumn("Net £", ExcelFormat.Currency),
            new ExcelColumn("Work order"));
        var queueLines = includeAllLines ? AllLines : QueueLines;
        foreach (var line in queueLines)
        {
            linesSheet.AddRow(
                line.Date,
                line.Supplier,
                line.InvoiceNumber,
                line.Description,
                line.CostCode,
                line.Net,
                WorkOrderText(line));
        }
        linesSheet.AddRow(includeAllLines ? "Total" : "Total shown", null, null, null, null, queueLines.Sum(line => line.Net), null);

        return workbook;
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        WorkOrders.OnChanged += StateHasChanged;
        // Refresh once per tab entry (stale-while-revalidate, per the front-end
        // data-loading convention) — the read model backs the dropdown's centre matching.
        _ = WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
        await RefreshAsync();
        isLoaded = true;
    }

    public void Dispose()
    {
        WorkOrders.OnChanged -= StateHasChanged;
    }
}
