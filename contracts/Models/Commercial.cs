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
    decimal CostCompletionPercent);

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
