using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpCommercialStore
{
    public IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId)
    {
        if (budgetsRequested.Add(projectId)) _ = LoadBudgetsAsync(projectId);
        return budgetsReadModel.Current(projectId);
    }

    private async Task LoadBudgetsAsync(string projectId)
    {
        try { await budgetsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { budgetsRequested.Remove(projectId); }
    }

    public CostCodeBudget SaveBudget(CostCodeBudget budget)
    {
        _ = SetBudgetAsync(budget);
        return budget;
    }

    private async Task SetBudgetAsync(CostCodeBudget budget)
    {
        await commands.SendAsync(new SetCostCodeBudget(budget.ProjectId, budget.CostCode, budget.AllocatedAmount, budget.SpentAmount), CancellationToken.None);
        await budgetsReadModel.RefreshAsync(budget.ProjectId, CancellationToken.None);
    }

    public IReadOnlyList<Timesheet> TimesheetsFor(string projectId)
    {
        if (timesheetsRequested.Add(projectId)) _ = LoadTimesheetsAsync(projectId);
        return timesheetsReadModel.Current(projectId);
    }

    private async Task LoadTimesheetsAsync(string projectId)
    {
        try { await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { timesheetsRequested.Remove(projectId); }
    }

    // SaveTimesheet/ApproveTimesheet were retired 2026-08-28 with the legacy Commercial
    // SubmitTimesheet/ApproveTimesheet slices — timesheet writes go through ILabourStore.
}
