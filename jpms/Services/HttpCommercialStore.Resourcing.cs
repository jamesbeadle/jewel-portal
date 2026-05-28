using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpCommercialStore
{
    public IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId)
    {
        if (budgetsReadModel.Current(projectId).Count == 0) _ = budgetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return budgetsReadModel.Current(projectId);
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
        if (timesheetsReadModel.Current(projectId).Count == 0) _ = timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return timesheetsReadModel.Current(projectId);
    }

    public Timesheet SaveTimesheet(Timesheet timesheet)
    {
        if (string.IsNullOrEmpty(timesheet.TimesheetId))
            _ = SubmitAsync(timesheet);
        return timesheet;
    }

    public Timesheet ApproveTimesheet(string timesheetId)
    {
        _ = commands.SendAsync(new ApproveTimesheet(timesheetId), CancellationToken.None);
        return new Timesheet(timesheetId, "", "", DateTimeOffset.UtcNow, 0, "", true);
    }

    private async Task SubmitAsync(Timesheet timesheet)
    {
        await commands.SendAsync(new SubmitTimesheet(timesheet.ProjectId, timesheet.PersonEmail, timesheet.WorkedOn, timesheet.Hours, timesheet.CostCode), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(timesheet.ProjectId, CancellationToken.None);
    }
}
