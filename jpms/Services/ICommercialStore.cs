using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICommercialStore
{
    IReadOnlyList<ClaimPeriod> ClaimPeriodsFor(string projectId);
    IReadOnlyList<Valuation> ValuationsFor(string projectId);
    IReadOnlyList<CvrSnapshot> CvrSnapshotsFor(string projectId);
    CvrSnapshot? LatestCvr(string projectId);
    IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId);
    IReadOnlyList<Timesheet> TimesheetsFor(string projectId);
    Timesheet SaveTimesheet(Timesheet timesheet);
    CashflowSnapshot LatestCashflow();
    event Action? OnChange;
}
