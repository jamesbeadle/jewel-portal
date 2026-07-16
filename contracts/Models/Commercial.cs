namespace Jewel.JPMS.Models;

public sealed record ClaimPeriod(
    string ClaimPeriodId,
    string ProjectId,
    int PeriodNumber,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate);

public sealed record Valuation(
    string ValuationId,
    string ClaimPeriodId,
    string ProjectId,
    decimal GrossValue,
    decimal RetentionPercent,
    decimal NetValue,
    bool IsIssued,
    DateTimeOffset? IssuedAt);

public sealed record CvrSnapshot(
    string CvrSnapshotId,
    string ProjectId,
    DateTimeOffset SnapshotAt,
    decimal TenderValue,
    decimal ForecastFinalCost,
    decimal ForecastFinalValue,
    decimal MarginPounds,
    decimal MarginPercent,
    int WeeksAheadOrBehind);

public sealed record CostCodeBudget(
    string CostCodeBudgetId,
    string ProjectId,
    string CostCode,
    decimal AllocatedAmount,
    decimal SpentAmount,
    decimal CommittedAmount = 0)   // committed-but-not-yet-spent, e.g. an approved variation order
{
    public decimal RemainingAmount => AllocatedAmount - SpentAmount;
    // What is left once both spend and outstanding commitments are taken off the allocation.
    public decimal UncommittedAmount => AllocatedAmount - SpentAmount - CommittedAmount;
    public bool IsOverrun => SpentAmount > AllocatedAmount;
}

/// <summary>Cost-side completion for one cost centre on one project (0–100),
/// edited inline on the Financials tab. Sales-side completion lives on the
/// valuation report's claims instead.</summary>
public sealed record CostCentreCostProgress(
    string CostCentreCostProgressId,
    string ProjectId,
    string CostCode,
    decimal CostCompletionPercent,
    bool IsFinalised = false);  // locked down: drawdown reads as realised profit / loss

/// <summary>One sales line's share inside a reconciliation package: the whole line's
/// value, or a partial £ amount when a client line covers more than one sub's scope
/// (the remainder stays available for other packages). Signed like the line.</summary>
public sealed record PackageSalesSlice(string ValuationLineItemId, decimal Amount);

/// <summary>One direct purchase cost's share inside a package: a £ slice of an allocated
/// Xero line not paying any work order — materials bought directly for the packaged
/// scope. Signed like the line's net (credit notes negative).</summary>
public sealed record PackageCostSlice(string XeroLedgerLineId, decimal Amount);

/// <summary>A reconciliation package: work orders and direct purchase costs (cost side)
/// against valuation sales lines (sales side), matched at the level the work was bought
/// and sold. Locked packages carry frozen snapshot figures; open ones compute live.</summary>
public sealed record ReconciliationPackage(
    string ReconciliationPackageId,
    string ProjectId,
    string Name,
    IReadOnlyList<string> WorkOrderIds,
    IReadOnlyList<PackageSalesSlice> SalesLines,
    bool IsLocked,
    DateTimeOffset? LockedAt,
    IReadOnlyList<PackageCostSlice>? CostLines = null)
{
    public IReadOnlyList<PackageCostSlice> DirectCosts => CostLines ?? Array.Empty<PackageCostSlice>();
}

/// <summary>One package's computed report row. Open packages compute live from source;
/// locked ones return the snapshot frozen at lock. Drawdown = target cost − WO committed
/// (budget left to commit). Margin is the live forecast buying gain: target cost less the
/// higher of committed and invoiced (so invoicing past the orders tightens it). Profit /
/// Loss is only realised on lock: target cost − actual invoiced cost.</summary>
public sealed record PackageReconciliationRow(
    string ReconciliationPackageId,
    string Name,
    bool IsLocked,
    DateTimeOffset? LockedAt,
    int WorkOrderCount,
    int SalesLineCount,
    decimal SalesValue,
    decimal ClaimedToDate,
    decimal TargetCost,
    decimal WoCommitted,
    decimal InvoicedToDate,
    decimal Drawdown,
    decimal Margin,
    decimal ProfitLoss,
    int CostLineCount = 0); // direct purchase slices on the cost side, alongside the orders

/// <summary>A named roll-up of cost centres shown as one line on the Financials tab.
/// Presentation only — figures are still stored per cost centre.</summary>
public sealed record CostCentreGroup(
    string CostCentreGroupId,
    string ProjectId,
    string Name,
    IReadOnlyList<string> CostCodes);

public sealed record Timesheet(
    string TimesheetId,
    string ProjectId,
    string PersonEmail,
    DateTimeOffset WorkedOn,
    decimal Hours,
    string CostCode,
    bool IsApproved);

public sealed record CashflowSnapshot(
    string CashflowSnapshotId,
    DateTimeOffset GeneratedAt,
    decimal ExpectedIncome13Week,
    decimal CommittedSpend13Week,
    decimal NetPosition13Week);
