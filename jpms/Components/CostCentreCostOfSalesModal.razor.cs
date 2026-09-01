
namespace Jewel.JPMS.Components;

public partial class CostCentreCostOfSalesModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string ProjectId { get; set; } = "";

    /// <summary>Modal heading: "CODE — Name" for one centre, or the roll-up's name.</summary>
    [Parameter] public string Heading { get; set; } = "";

    /// <summary>The cost codes behind the clicked figure — one for an individual row,
    /// all members for a roll-up (the Centre column appears when there are several).</summary>
    [Parameter] public IReadOnlyList<string> CostCodes { get; set; } = Array.Empty<string>();

    /// <summary>The project's work orders (the same store the Work Orders tab renders) —
    /// the link dropdown offers those with lines coded to the invoice line's centre.</summary>
    [Parameter] public IReadOnlyList<ProjectWorkOrderDetail> Orders { get; set; } = Array.Empty<ProjectWorkOrderDetail>();

    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Raised after a line has been reallocated to another cost centre,
    /// so the parent can close the modal and refresh the financial summary.</summary>
    [Parameter] public EventCallback OnInvoiceMoved { get; set; }

    /// <summary>Raised after a line's work-order link changed, so the parent can refresh
    /// the financial summary (the non-WO cost of sales and drawdown move). The modal
    /// stays open — linking is usually done for several lines in a row.</summary>
    [Parameter] public EventCallback OnWorkOrderLinkChanged { get; set; }

    // Each fetched line tagged with the centre it came from (the per-centre query
    // doesn't echo the code back).
    private sealed record Entry(string CostCode, CostCentreActualCostLine Line);

    private List<Entry> entries = new();
    // Per-order invoiced balances, fetched with the lines and refreshed after every link
    // change — the dropdown shows each order's remaining balance and disables orders the
    // line can't fit into (the API hard-blocks over-invoicing regardless).
    private Dictionary<string, WorkOrderInvoiceSummary> summariesByOrder = new(StringComparer.OrdinalIgnoreCase);
    private bool loading;
    private string? loadedKey;
    private string? movingLineId;
    private bool isMoving;
    private string? moveError;
    // The lines whose links are being saved — only those rows' dropdowns disable, so a slow
    // call (e.g. the serverless database warming up) doesn't lock the whole table, and other
    // rows stay linkable in the meantime.
    private readonly HashSet<string> linkingLineIds = new();
    private string? linkError;
    // Bumped when a save fails, recreating the selects so they fall back to the stored link.
    private int linkRevertNonce;
    // The line whose amount split is open in the split editor, if any.
    private Entry? splitEntry;

    private bool ShowCentreColumn => CostCodes.Count > 1;

    // Invoice lines allocated to this centre (editable) vs the ± rows that mirror the
    // summary's work-order re-attribution (read-only). Together they total the column.
    private List<Entry> ResidentEntries => entries.Where(entry => !entry.Line.IsWorkOrderAttribution).ToList();
    private List<Entry> AttributionEntries => entries.Where(entry => entry.Line.IsWorkOrderAttribution).ToList();

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen)
        {
            // Refetch on next open so the detail always matches the live table.
            loadedKey = null;
            movingLineId = null;
            moveError = null;
            linkError = null;
            return;
        }
        var key = $"{ProjectId}|{string.Join("|", CostCodes)}";
        if (key == loadedKey) return;
        loadedKey = key;
        loading = true;
        entries = new List<Entry>();
        foreach (var costCode in CostCodes)
        {
            var lines = await Queries.AskAsync(new ListCostCentreActualCosts(ProjectId, costCode), CancellationToken.None);
            entries.AddRange(lines.Select(line => new Entry(costCode, line)));
        }
        entries = entries.OrderBy(entry => entry.Line.Date ?? DateTime.MaxValue).ToList();
        await RefreshSummariesAsync();
        loading = false;
    }

    private async Task RefreshSummariesAsync()
    {
        var summaries = await Queries.AskAsync(new ListWorkOrderInvoiceSummaries(ProjectId), CancellationToken.None);
        summariesByOrder = summaries.ToDictionary(summary => summary.WorkOrderId, StringComparer.OrdinalIgnoreCase);
    }

    private decimal? RemainingFor(string workOrderId) =>
        summariesByOrder.TryGetValue(workOrderId, out var summary) ? summary.RemainingToInvoice : null;

    // A single full-amount slice renders as the plain dropdown; anything else (several
    // orders, or one partial slice) is an amount split the dropdown can't express.
    private static string? SingleLinkId(CostCentreActualCostLine line) =>
        line.Links.Count == 1 ? line.Links[0].WorkOrderId : null;

    private static bool IsAmountSplit(CostCentreActualCostLine line) =>
        line.Links.Count > 1 || (line.Links.Count == 1 && line.Links[0].Amount != line.Net);

    private string SplitTitle(CostCentreActualCostLine line) =>
        string.Join(" · ", line.Links.Select(link =>
            summariesByOrder.TryGetValue(link.WorkOrderId, out var summary)
                ? $"WO-{summary.Number:0000} {MoneyExact(link.Amount)}"
                : MoneyExact(link.Amount)))
        + (line.Net - line.LinkedTotal == 0m ? "" : $" · {MoneyExact(line.Net - line.LinkedTotal)} not linked");

    // The split editor offers the same orders as the dropdown, with live balances.
    private IReadOnlyList<WorkOrderInvoiceSummary> SplitOptions =>
        LinkableWorkOrders
            .Select(detail => summariesByOrder.TryGetValue(detail.Order.WorkOrderId, out var summary) ? summary : null)
            .OfType<WorkOrderInvoiceSummary>()
            .ToList();

    private async Task HandleSplitSavedAsync(IReadOnlyList<XeroWorkOrderLinkSlice> slices)
    {
        if (splitEntry is not null)
        {
            var index = entries.FindIndex(candidate => candidate.Line.XeroLedgerLineId == splitEntry.Line.XeroLedgerLineId);
            if (index >= 0) entries[index] = entries[index] with { Line = entries[index].Line with { WorkOrderLinks = slices } };
        }
        splitEntry = null;
        await RefreshSummariesAsync(); // the balances the dropdowns show just moved
        await OnWorkOrderLinkChanged.InvokeAsync();
    }

    private void CancelMove()
    {
        movingLineId = null;
        moveError = null;
    }

    // Work orders with any line coded to any of THIS REPORT LINE's centres — a short
    // list that's hard to mislink. Scoped to the whole line rather than the invoice's
    // own centre: on a roll-up, the invoice often sits on a sibling centre of the one
    // the work order is coded to (e.g. timber-windows invoice paying an aluminium-
    // windows order inside a "Windows, doors and glazing" group).
    // Cancelled, draft and rejected orders are omitted — the API refuses links to all three.
    private IEnumerable<ProjectWorkOrderDetail> LinkableWorkOrders =>
        Orders.Where(detail => detail.Order.Status is not (WorkOrderStatus.Cancelled or WorkOrderStatus.Draft or WorkOrderStatus.Rejected)
                               && detail.Lines.Any(line =>
                CostCodes.Contains(line.CostCode, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(detail => detail.Order.Number);

    private async Task LinkAsync(Entry entry, string? workOrderId)
    {
        if (!linkingLineIds.Add(entry.Line.XeroLedgerLineId)) return;
        var newLink = string.IsNullOrEmpty(workOrderId) ? null : workOrderId;
        if (newLink == SingleLinkId(entry.Line) && !IsAmountSplit(entry.Line))
        {
            linkingLineIds.Remove(entry.Line.XeroLedgerLineId);
            return;
        }
        linkError = null;
        var slices = newLink is null
            ? (IReadOnlyList<XeroWorkOrderLinkSlice>)Array.Empty<XeroWorkOrderLinkSlice>()
            : new[] { new XeroWorkOrderLinkSlice(newLink, entry.Line.Net) }; // whole line to one order
        try
        {
            await Commands.SendAsync(
                new SetXeroLineWorkOrderLinks(ProjectId, entry.Line.XeroLedgerLineId, slices), CancellationToken.None);
            var index = entries.FindIndex(candidate => candidate.Line.XeroLedgerLineId == entry.Line.XeroLedgerLineId);
            if (index >= 0) entries[index] = entries[index] with { Line = entries[index].Line with { WorkOrderLinks = slices } };
            await RefreshSummariesAsync(); // the balances the dropdowns show just moved
            await OnWorkOrderLinkChanged.InvokeAsync();
        }
        catch (CommandFailedException ex)
        {
            linkError = $"Couldn't link this line: {ex.Message}";
            linkRevertNonce++; // snap the dropdown back to the stored link — the save didn't happen
        }
        finally
        {
            linkingLineIds.Remove(entry.Line.XeroLedgerLineId);
        }
    }

    private async Task MoveAsync(Entry entry, string? newCostCode)
    {
        if (string.IsNullOrEmpty(newCostCode) || isMoving) return;
        isMoving = true;
        moveError = null;
        try
        {
            // Note: SetXeroAllocation replaces the line's Note, so record the move there.
            await Ledger.ApplyAsync(new SetXeroAllocation(
                new[] { entry.Line.XeroLedgerLineId },
                XeroAllocationAction.Allocate,
                ProjectId,
                newCostCode,
                Note: $"Moved from {entry.CostCode} on the Financials tab"));
            movingLineId = null;
            await OnInvoiceMoved.InvokeAsync();
        }
        catch (Exception ex)
        {
            moveError = $"Couldn't move this line: {ex.Message}";
        }
        finally
        {
            isMoving = false;
        }
    }

    private static string MoneyExact(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
}
