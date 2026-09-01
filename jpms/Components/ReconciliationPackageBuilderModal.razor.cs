using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Components;

public partial class ReconciliationPackageBuilderModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string ProjectId { get; set; } = "";

    /// <summary>The package being edited, or null when creating.</summary>
    [Parameter] public ReconciliationPackage? Editing { get; set; }

    /// <summary>Every package on the project — for one-home checks and remaining-available maths.</summary>
    [Parameter] public IReadOnlyList<ReconciliationPackage> AllPackages { get; set; } = Array.Empty<ReconciliationPackage>();

    [Parameter] public IReadOnlyList<ValuationLineItem> ValuationLines { get; set; } = Array.Empty<ValuationLineItem>();
    [Parameter] public IReadOnlyList<ProjectWorkOrderDetail> Orders { get; set; } = Array.Empty<ProjectWorkOrderDetail>();

    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private string name = "";
    private string orderSearch = "";
    private string lineSearch = "";
    private string costSearch = "";
    private readonly HashSet<string> selectedOrderIds = new(StringComparer.OrdinalIgnoreCase);
    // Line id → this package's £ share, as typed (invariant decimal text).
    private readonly Dictionary<string, string> pickedAmounts = new(StringComparer.OrdinalIgnoreCase);
    // Xero ledger line id → this package's direct-cost £ share, as typed.
    private readonly Dictionary<string, string> pickedCostAmounts = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<ProjectCostOfSalesLine> costLines = Array.Empty<ProjectCostOfSalesLine>();
    private bool costLinesLoading;
    private string? seededForKey;
    private bool busy;
    private string? saveError;

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen)
        {
            seededForKey = null;
            saveError = null;
            return;
        }
        var key = Editing?.ReconciliationPackageId ?? "(new)";
        if (seededForKey == key) return;
        seededForKey = key;
        saveError = null;
        name = Editing?.Name ?? "";
        selectedOrderIds.Clear();
        pickedAmounts.Clear();
        pickedCostAmounts.Clear();
        if (Editing is not null)
        {
            foreach (var workOrderId in Editing.WorkOrderIds) selectedOrderIds.Add(workOrderId);
            foreach (var slice in Editing.SalesLines)
                pickedAmounts[slice.ValuationLineItemId] = slice.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (var slice in Editing.DirectCosts)
                pickedCostAmounts[slice.XeroLedgerLineId] = slice.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        // The direct-cost picker's queue: every allocated purchase line with its
        // work-order links, fetched fresh each open so availability is current.
        costLinesLoading = true;
        try { costLines = await Queries.AskAsync(new ListProjectCostOfSalesLines(ProjectId), CancellationToken.None); }
        catch { costLines = Array.Empty<ProjectCostOfSalesLine>(); }
        costLinesLoading = false;
    }

    // Which OTHER package owns this order, if any (one-home rule shown, not just enforced).
    private string? OwnerOf(string workOrderId) =>
        AllPackages.FirstOrDefault(package =>
                package.ReconciliationPackageId != (Editing?.ReconciliationPackageId ?? "")
                && package.WorkOrderIds.Contains(workOrderId, StringComparer.OrdinalIgnoreCase))
            ?.Name;

    private static decimal CommittedOf(ProjectWorkOrderDetail detail) => detail.Lines.Sum(line => line.LineTotal);

    // Cancelled and rejected orders are gone; drafts aren't approved scope yet — none belong in a package.
    private List<ProjectWorkOrderDetail> FilteredOrders =>
        Orders.Where(detail => detail.Order.Status is not (WorkOrderStatus.Cancelled or WorkOrderStatus.Draft or WorkOrderStatus.Rejected))
            .Where(detail => string.IsNullOrWhiteSpace(orderSearch)
                             || detail.SubcontractorName.Contains(orderSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                             || detail.Order.Title.Contains(orderSearch.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(detail => detail.Order.Number)
            .ToList();

    // Counting lines with how much of each is still available to THIS package
    // (the line's value less what other packages have taken).
    private List<(ValuationLineItem Line, decimal AvailableElsewhere)> FilteredLines
    {
        get
        {
            var takenByOthers = AllPackages
                .Where(package => package.ReconciliationPackageId != (Editing?.ReconciliationPackageId ?? ""))
                .SelectMany(package => package.SalesLines)
                .GroupBy(slice => slice.ValuationLineItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(slice => slice.Amount), StringComparer.OrdinalIgnoreCase);
            return ValuationLines
                .Where(line => line.CountsTowardTotals && line.LineAmount != 0m)
                .Where(line => string.IsNullOrWhiteSpace(lineSearch)
                               || line.Description.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.CostCode.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                               || line.SectionName.Contains(lineSearch.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(line => (Line: line,
                    AvailableElsewhere: line.LineAmount - (takenByOthers.TryGetValue(line.ValuationLineItemId, out var taken) ? taken : 0m)))
                .OrderBy(entry => entry.Line.CostCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Line.DisplayOrder)
                .ToList();
        }
    }

    private void ToggleOrder(string workOrderId)
    {
        if (!selectedOrderIds.Remove(workOrderId)) selectedOrderIds.Add(workOrderId);
    }

    private void ToggleLine((ValuationLineItem Line, decimal AvailableElsewhere) entry)
    {
        if (pickedAmounts.Remove(entry.Line.ValuationLineItemId)) return;
        // Whole-line default: the full remaining value; edit the amount for a partial share.
        pickedAmounts[entry.Line.ValuationLineItemId] =
            entry.AvailableElsewhere.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void SetLineAmount(string lineItemId, string? text) => pickedAmounts[lineItemId] = text ?? "";

    // Direct purchase invoices with how much of each is still available to THIS package:
    // the line's net less its work-order link slices (that spend arrives through the
    // orders) and less what other packages' direct slices have taken. Split lines can't
    // join a package (same rule as links), and lines with nothing left are shown only
    // while ticked here.
    private decimal TakenByOtherPackages(string xeroLedgerLineId) =>
        AllPackages
            .Where(package => package.ReconciliationPackageId != (Editing?.ReconciliationPackageId ?? ""))
            .SelectMany(package => package.DirectCosts)
            .Where(slice => string.Equals(slice.XeroLedgerLineId, xeroLedgerLineId, StringComparison.OrdinalIgnoreCase))
            .Sum(slice => slice.Amount);

    private List<(ProjectCostOfSalesLine Line, decimal Available)> FilteredCostLines =>
        costLines
            .Where(line => !line.IsSplit)
            .Where(line => string.IsNullOrWhiteSpace(costSearch)
                           || line.Supplier.Contains(costSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                           || line.InvoiceNumber.Contains(costSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                           || line.Description.Contains(costSearch.Trim(), StringComparison.OrdinalIgnoreCase)
                           || line.CostCode.Contains(costSearch.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(line => (Line: line, Available: line.UnlinkedRemainder - TakenByOtherPackages(line.XeroLedgerLineId)))
            .Where(entry => entry.Available != 0m || pickedCostAmounts.ContainsKey(entry.Line.XeroLedgerLineId))
            .OrderByDescending(entry => Math.Abs(entry.Available))
            .ThenByDescending(entry => entry.Line.Date)
            .ToList();

    private void ToggleCostLine((ProjectCostOfSalesLine Line, decimal Available) entry)
    {
        if (pickedCostAmounts.Remove(entry.Line.XeroLedgerLineId)) return;
        // Whole-remainder default; edit the amount for a partial share.
        pickedCostAmounts[entry.Line.XeroLedgerLineId] =
            entry.Available.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void SetCostAmount(string xeroLedgerLineId, string? text) => pickedCostAmounts[xeroLedgerLineId] = text ?? "";

    private static decimal? Parse(string text) =>
        decimal.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private decimal SalesTotal => pickedAmounts.Values.Sum(text => Parse(text) ?? 0m);
    private decimal TargetTotal => Math.Round(SalesTotal * FinancialSummaryAssumptions.CostFactor, 2);
    private decimal CommittedTotal =>
        Orders.Where(detail => selectedOrderIds.Contains(detail.Order.WorkOrderId)).Sum(CommittedOf);
    private decimal DirectCostTotal => pickedCostAmounts.Values.Sum(text => Parse(text) ?? 0m);
    private decimal Difference => TargetTotal - CommittedTotal - DirectCostTotal;

    private static string DivisorText =>
        $"{1m + FinancialSummaryAssumptions.MarkupPercent / 100m:0.##}";

    private string? ClientValidationError
    {
        get
        {
            if (string.IsNullOrWhiteSpace(name)) return null; // save button already disabled; don't nag
            if (pickedAmounts.Values.Any(text => Parse(text) is not { } amount || amount == 0m))
                return "Every ticked sales line needs a non-zero amount.";
            var linesById = ValuationLines.ToDictionary(line => line.ValuationLineItemId, StringComparer.OrdinalIgnoreCase);
            foreach (var picked in pickedAmounts)
            {
                if (!linesById.TryGetValue(picked.Key, out var line)) continue;
                var amount = Parse(picked.Value)!.Value;
                if (Math.Sign(amount) != Math.Sign(line.LineAmount))
                    return $"\"{Truncate(line.Description, 40)}\" — the share must carry the line's sign ({Money(line.LineAmount)}).";
                var available = FilteredLinesAvailable(picked.Key, line);
                if (Math.Abs(amount) > Math.Abs(available))
                    return $"\"{Truncate(line.Description, 40)}\" — only {Money(available)} of the line is still available.";
            }
            if (pickedCostAmounts.Values.Any(text => Parse(text) is not { } costAmount || costAmount == 0m))
                return "Every ticked purchase invoice needs a non-zero amount.";
            var costLinesById = costLines.ToDictionary(line => line.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase);
            foreach (var picked in pickedCostAmounts)
            {
                if (!costLinesById.TryGetValue(picked.Key, out var costLine)) continue;
                var amount = Parse(picked.Value)!.Value;
                if (costLine.Net != 0m && Math.Sign(amount) != Math.Sign(costLine.Net))
                    return $"{costLine.Supplier} {costLine.InvoiceNumber} — the share must carry the line's sign ({Money(costLine.Net)}).";
                var costAvailable = costLine.UnlinkedRemainder - TakenByOtherPackages(picked.Key);
                if (Math.Abs(amount) > Math.Abs(costAvailable))
                    return $"{costLine.Supplier} {costLine.InvoiceNumber} — only {Money(costAvailable)} is not already paying a work order or in another package.";
            }
            return null;
        }
    }

    private decimal FilteredLinesAvailable(string lineItemId, ValuationLineItem line)
    {
        var takenByOthers = AllPackages
            .Where(package => package.ReconciliationPackageId != (Editing?.ReconciliationPackageId ?? ""))
            .SelectMany(package => package.SalesLines)
            .Where(slice => string.Equals(slice.ValuationLineItemId, lineItemId, StringComparison.OrdinalIgnoreCase))
            .Sum(slice => slice.Amount);
        return line.LineAmount - takenByOthers;
    }

    private bool CanSave =>
        !string.IsNullOrWhiteSpace(name)
        && (selectedOrderIds.Count > 0 || pickedAmounts.Count > 0 || pickedCostAmounts.Count > 0)
        && ClientValidationError is null;

    private async Task SaveAsync()
    {
        if (busy || !CanSave) return;
        busy = true;
        saveError = null;
        var slices = new List<PackageSalesSlice>();
        foreach (var picked in pickedAmounts)
        {
            var amount = Parse(picked.Value);
            if (amount is null || amount.Value == 0m) continue;
            slices.Add(new PackageSalesSlice(picked.Key, amount.Value));
        }
        var costSlices = new List<PackageCostSlice>();
        foreach (var picked in pickedCostAmounts)
        {
            var amount = Parse(picked.Value);
            if (amount is null || amount.Value == 0m) continue;
            costSlices.Add(new PackageCostSlice(picked.Key, amount.Value));
        }
        try
        {
            await Commands.SendAsync(new SaveReconciliationPackage(
                ProjectId,
                Editing?.ReconciliationPackageId,
                name.Trim(),
                selectedOrderIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList(),
                slices,
                costSlices), CancellationToken.None);
            seededForKey = null; // reseed from saved state on next open
            await OnSaved.InvokeAsync();
        }
        catch (CommandFailedException ex)
        {
            saveError = $"Couldn't save the package: {ex.Message}";
        }
        finally
        {
            busy = false;
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

}
