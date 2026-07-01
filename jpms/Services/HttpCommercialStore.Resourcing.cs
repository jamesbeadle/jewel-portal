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
