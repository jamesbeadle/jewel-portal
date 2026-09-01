using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Components;

public partial class PackageReconciliationSection
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    /// <summary>The project's valuation lines (already loaded by the Financials page).</summary>
    [Parameter] public IReadOnlyList<ValuationLineItem> ValuationLines { get; set; } = Array.Empty<ValuationLineItem>();

    /// <summary>The project's work orders (already loaded by the Financials page).</summary>
    [Parameter] public IReadOnlyList<ProjectWorkOrderDetail> Orders { get; set; } = Array.Empty<ProjectWorkOrderDetail>();

    /// <summary>Raised with the computed package rows whenever they (re)load, so the
    /// page can show them as lines in the main Financials table without a second fetch.</summary>
    [Parameter] public EventCallback<IReadOnlyList<PackageReconciliationRow>> OnRowsChanged { get; set; }

    /// <summary>Raised when the manual work-order flow raises a new order, so the page
    /// can refresh its work-orders store (the WO Committed column reads from it).</summary>
    [Parameter] public EventCallback OnOrdersChanged { get; set; }

    private IReadOnlyList<ReconciliationPackage> packages = Array.Empty<ReconciliationPackage>();
    private IReadOnlyList<PackageReconciliationRow> rows = Array.Empty<PackageReconciliationRow>();
    private bool loading = true;
    private bool busy;
    private string? error;
    private bool showUnallocated;
    private string? pendingDeleteId;
    private bool builderOpen;
    private ReconciliationPackage? editing;

    private bool CanManage => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager or Role.QuantitySurveyor);

    private HashSet<string> PackagedOrderIds =>
        packages.SelectMany(package => package.WorkOrderIds).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private List<ProjectWorkOrderDetail> UnpackagedOrders =>
        Orders.Where(detail => detail.Order.Status is not (WorkOrderStatus.Cancelled or WorkOrderStatus.Draft or WorkOrderStatus.Rejected)
                               && !PackagedOrderIds.Contains(detail.Order.WorkOrderId))
            .OrderBy(detail => detail.Order.Number)
            .ToList();

    // Counting sales lines with value not yet assigned to any package.
    private List<(ValuationLineItem Line, decimal Remaining)> LinesWithRemaining
    {
        get
        {
            var assignedByLine = packages
                .SelectMany(package => package.SalesLines)
                .GroupBy(slice => slice.ValuationLineItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(slice => slice.Amount), StringComparer.OrdinalIgnoreCase);
            return ValuationLines
                .Where(line => line.CountsTowardTotals && line.LineAmount != 0m)
                .Select(line => (Line: line,
                    Remaining: line.LineAmount - (assignedByLine.TryGetValue(line.ValuationLineItemId, out var assigned) ? assigned : 0m)))
                .Where(entry => entry.Remaining != 0m)
                .OrderByDescending(entry => Math.Abs(entry.Remaining))
                .ToList();
        }
    }

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            var definitionsTask = Queries.AskAsync(new ListReconciliationPackagesForProject(ProjectId), CancellationToken.None);
            var rowsTask = Queries.AskAsync(new ListPackageReconciliation(ProjectId), CancellationToken.None);
            packages = await definitionsTask;
            rows = await rowsTask;
            error = null;
            await OnRowsChanged.InvokeAsync(rows);
        }
        catch { error = "Couldn't load packages. Please try again."; }
        loading = false;
    }

    private void OpenCreate()
    {
        editing = null;
        builderOpen = true;
    }

    private void OpenEdit(string packageId)
    {
        editing = packages.FirstOrDefault(package =>
            string.Equals(package.ReconciliationPackageId, packageId, StringComparison.OrdinalIgnoreCase));
        builderOpen = editing is not null;
    }

    private void CloseBuilder()
    {
        builderOpen = false;
        editing = null;
    }

    private async Task HandleSavedAsync()
    {
        builderOpen = false;
        editing = null;
        await ReloadAsync();
    }

    // The manual "add work order (+ package)" flow saved: a new order now exists, and
    // possibly a new package holding it — refresh both sides.
    private bool manualOrderOpen;

    private async Task HandleManualOrderSavedAsync()
    {
        manualOrderOpen = false;
        await OnOrdersChanged.InvokeAsync();
        await ReloadAsync();
    }

    private async Task SetLockAsync(PackageReconciliationRow row, bool locked)
    {
        if (busy) return;
        busy = true;
        error = null;
        try
        {
            await Commands.SendAsync(
                new SetReconciliationPackageLock(ProjectId, row.ReconciliationPackageId, locked), CancellationToken.None);
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        finally { busy = false; }
    }

    private async Task RemoveAsync(PackageReconciliationRow row)
    {
        if (busy) return;
        busy = true;
        error = null;
        try
        {
            await Commands.SendAsync(
                new RemoveReconciliationPackage(ProjectId, row.ReconciliationPackageId), CancellationToken.None);
            pendingDeleteId = null;
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        finally { busy = false; }
    }

    // Matches the rendered rows exactly: same package rows, same locked/open figure
    // per column (Drawdown / Margin blank while locked, Profit / loss shown instead).
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        if (rows.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Reconciliation packages",
            new ExcelColumn("Package"),
            new ExcelColumn("Sales value", ExcelFormat.Currency),
            new ExcelColumn("Claimed", ExcelFormat.Currency),
            new ExcelColumn("Target cost", ExcelFormat.Currency),
            new ExcelColumn("WO committed", ExcelFormat.Currency),
            new ExcelColumn("Invoiced", ExcelFormat.Currency),
            new ExcelColumn("Drawdown", ExcelFormat.Currency),
            new ExcelColumn("Margin / P&L", ExcelFormat.Currency));

        foreach (var row in rows)
        {
            sheet.AddRow(
                row.Name,
                row.SalesValue,
                row.ClaimedToDate,
                row.TargetCost,
                row.WoCommitted,
                row.InvoicedToDate,
                row.IsLocked ? (decimal?)null : row.Drawdown,
                row.IsLocked ? (decimal?)row.ProfitLoss : row.Margin);
        }
        return workbook;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

}
