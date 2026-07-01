using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Site;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpSiteStore : ISiteStore
{
    private readonly SiteReportsReadModel reportsReadModel;
    private readonly ProgrammeReadModel programmeReadModel;
    private readonly ICommandSender commands;

    // Projects whose data has had a load started — prevents an empty result
    // from re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> reportsRequested = new();
    private readonly HashSet<string> programmeRequested = new();

    public HttpSiteStore(SiteReportsReadModel reportsReadModel, ProgrammeReadModel programmeReadModel, ICommandSender commands)
    {
        this.reportsReadModel = reportsReadModel;
        this.programmeReadModel = programmeReadModel;
        this.commands = commands;
        reportsReadModel.OnChanged += () => OnChange?.Invoke();
        programmeReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<SiteReport> ReportsFor(string projectId)
    {
        if (reportsRequested.Add(projectId)) _ = LoadReportsAsync(projectId);
        return reportsReadModel.Current(projectId);
    }

    private async Task LoadReportsAsync(string projectId)
    {
        try { await reportsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { reportsRequested.Remove(projectId); }
    }

    public IReadOnlyList<ProgrammeTask> ProgrammeFor(string projectId)
    {
        if (programmeRequested.Add(projectId)) _ = LoadProgrammeAsync(projectId);
        return programmeReadModel.Current(projectId);
    }

    private async Task LoadProgrammeAsync(string projectId)
    {
        try { await programmeReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { programmeRequested.Remove(projectId); }
    }

    public SiteReport SaveReport(SiteReport report)
    {
        if (string.IsNullOrEmpty(report.SiteReportId))
            _ = AssembleAsync(report);
        else if (report.IsIssued)
            _ = ApproveAsync(report);
        return report;
    }

    public ProgrammeTask SaveProgrammeTask(ProgrammeTask task)
    {
        if (string.IsNullOrEmpty(task.ProgrammeTaskId))
            _ = AddTaskAsync(task);
        else
            _ = UpdateTaskAsync(task);
        return task;
    }

    private async Task AssembleAsync(SiteReport report)
    {
        await commands.SendAsync(new AssembleSiteReport(report.ProjectId, report.PeriodEnd, report.Narrative, report.AttendanceDays, report.OpenSnags, report.ProgressPercent), CancellationToken.None);
        await reportsReadModel.RefreshAsync(report.ProjectId, CancellationToken.None);
    }

    private async Task ApproveAsync(SiteReport report)
    {
        await commands.SendAsync(new ApproveSiteReport(report.SiteReportId), CancellationToken.None);
        await reportsReadModel.RefreshAsync(report.ProjectId, CancellationToken.None);
    }

    private async Task AddTaskAsync(ProgrammeTask task)
    {
        await commands.SendAsync(new AddProgrammeTask(task.ProjectId, task.Title, task.PlannedStart, task.PlannedEnd, task.BoqLineItemId), CancellationToken.None);
        await programmeReadModel.RefreshAsync(task.ProjectId, CancellationToken.None);
    }

    private async Task UpdateTaskAsync(ProgrammeTask task)
    {
        await commands.SendAsync(new UpdateProgrammeTask(task.ProgrammeTaskId, task.Title, task.PlannedStart, task.PlannedEnd, task.ProgressPercent, task.BoqLineItemId), CancellationToken.None);
        await programmeReadModel.RefreshAsync(task.ProjectId, CancellationToken.None);
    }
}
