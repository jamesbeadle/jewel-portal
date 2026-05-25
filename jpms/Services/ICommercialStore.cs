using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICommercialStore
{
    IReadOnlyList<ClaimPeriod> ClaimPeriodsFor(string projectId);

    IReadOnlyList<Valuation> ValuationsFor(string projectId);
    Valuation SaveValuation(Valuation valuation);

    IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId);

    IReadOnlyList<Timesheet> TimesheetsFor(string projectId);
    Timesheet SaveTimesheet(Timesheet timesheet);
    Timesheet ApproveTimesheet(string timesheetId);

    CashflowSnapshot LatestCashflow();

    event Action? OnChange;
}
