using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICommercialStore
{
    Task<IReadOnlyList<ClaimPeriod>> ClaimPeriodsForAsync(string projectId);

    IReadOnlyList<Valuation> ValuationsFor(string projectId);
    Valuation SaveValuation(Valuation valuation);

    IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId);
    CostCodeBudget SaveBudget(CostCodeBudget budget);

    IReadOnlyList<Timesheet> TimesheetsFor(string projectId);
    Timesheet SaveTimesheet(Timesheet timesheet);
    Timesheet ApproveTimesheet(string timesheetId);

    CashflowSnapshot LatestCashflow();

    event Action? OnChange;
}
