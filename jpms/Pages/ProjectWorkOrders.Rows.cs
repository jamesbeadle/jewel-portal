using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using static Jewel.JPMS.Features.Procurement.WorkOrderDisplay;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{
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

    private void Toggle(string key)
    {
        if (!expandedKeys.Remove(key)) expandedKeys.Add(key);
    }

    private static string Reference(WorkOrder order) => order.Reference;
}
