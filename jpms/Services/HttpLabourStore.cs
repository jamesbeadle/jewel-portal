using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Labour;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpLabourStore : ILabourStore
{
    private readonly WorkersReadModel workersReadModel;
    private readonly WorkerAssignmentsReadModel assignmentsReadModel;
    private readonly LabourTimesheetsReadModel timesheetsReadModel;
    private readonly SiteAttendanceReadModel attendanceReadModel;
    private readonly MyLabourDayReadModel myDayReadModel;
    private readonly LabourSettlementReadModel settlementReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Keys with a load already started — an empty result must not re-trigger a fetch on every
    // re-render (see HttpDrawingStore / CLAUDE.md front-end data-loading convention).
    private bool workersRequested;
    private readonly HashSet<string> assignmentsRequested = new();
    private readonly HashSet<string> timesheetsRequested = new();
    private readonly HashSet<string> attendanceRequested = new();
    private bool myDayRequested;
    private readonly HashSet<string> settlementRequested = new();

    public HttpLabourStore(WorkersReadModel workersReadModel, WorkerAssignmentsReadModel assignmentsReadModel,
        LabourTimesheetsReadModel timesheetsReadModel, SiteAttendanceReadModel attendanceReadModel,
        MyLabourDayReadModel myDayReadModel, LabourSettlementReadModel settlementReadModel,
        IQueryClient queries, ICommandSender commands)
    {
        this.workersReadModel = workersReadModel;
        this.assignmentsReadModel = assignmentsReadModel;
        this.timesheetsReadModel = timesheetsReadModel;
        this.attendanceReadModel = attendanceReadModel;
        this.myDayReadModel = myDayReadModel;
        this.settlementReadModel = settlementReadModel;
        this.queries = queries;
        this.commands = commands;
        workersReadModel.OnChanged += () => OnChange?.Invoke();
        assignmentsReadModel.OnChanged += () => OnChange?.Invoke();
        timesheetsReadModel.OnChanged += () => OnChange?.Invoke();
        attendanceReadModel.OnChanged += () => OnChange?.Invoke();
        myDayReadModel.OnChanged += () => OnChange?.Invoke();
        settlementReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Worker> Workers()
    {
        if (!workersRequested) { workersRequested = true; _ = LoadWorkersAsync(); }
        return workersReadModel.Current;
    }

    private async Task LoadWorkersAsync()
    {
        try { await workersReadModel.RefreshAsync(CancellationToken.None); }
        catch { workersRequested = false; }
    }

    public Task RefreshWorkersAsync() => workersReadModel.RefreshAsync(CancellationToken.None);

    public async Task<Worker> AddWorkerAsync(string name, decimal hourlyRate, string? subcontractorId, string contactEmail, string contactPhone)
    {
        var worker = await commands.SendAsync(new AddWorker(name, hourlyRate, subcontractorId, contactEmail, contactPhone), CancellationToken.None);
        await workersReadModel.RefreshAsync(CancellationToken.None);
        return worker;
    }

    public async Task<Worker> UpdateWorkerAsync(Worker worker)
    {
        var updated = await commands.SendAsync(new UpdateWorker(worker.WorkerId, worker.Name, worker.HourlyRate,
            worker.IsActive, worker.SubcontractorId, worker.ContactEmail, worker.ContactPhone), CancellationToken.None);
        await workersReadModel.RefreshAsync(CancellationToken.None);
        return updated;
    }

    public IReadOnlyList<ProjectWorkerAssignment> AssignmentsFor(string projectId)
    {
        if (assignmentsRequested.Add(projectId)) _ = LoadAsync(() => assignmentsReadModel.RefreshAsync(projectId, CancellationToken.None), assignmentsRequested, projectId);
        return assignmentsReadModel.Current(projectId);
    }

    public Task RefreshAssignmentsAsync(string projectId) => assignmentsReadModel.RefreshAsync(projectId, CancellationToken.None);

    public async Task SetAssignmentAsync(string projectId, string workerId, bool isActive)
    {
        await commands.SendAsync(new SetProjectWorkerAssignment(projectId, workerId, isActive), CancellationToken.None);
        await assignmentsReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    public MyLabourDay? MyDay()
    {
        if (!myDayRequested) { myDayRequested = true; _ = LoadMyDayAsync(); }
        return myDayReadModel.Current;
    }

    private async Task LoadMyDayAsync()
    {
        try { await myDayReadModel.RefreshAsync(CancellationToken.None); }
        catch { myDayRequested = false; }
    }

    public Task RefreshMyDayAsync() => myDayReadModel.RefreshAsync(CancellationToken.None);

    public async Task MySignInAsync(string projectId)
    {
        await commands.SendAsync(new MySiteSignIn(projectId), CancellationToken.None);
        await myDayReadModel.RefreshAsync(CancellationToken.None);
    }

    public async Task MySignOutAsync(string projectId, IReadOnlyList<SiteSignOutEntry> entries)
    {
        await commands.SendAsync(new MySiteSignOut(projectId, entries), CancellationToken.None);
        await myDayReadModel.RefreshAsync(CancellationToken.None);
    }

    public async Task MyResubmitAsync(string timesheetId, decimal hours, string costCode)
    {
        await commands.SendAsync(new MyResubmitTimesheet(timesheetId, hours, costCode), CancellationToken.None);
        await myDayReadModel.RefreshAsync(CancellationToken.None);
    }

    public IReadOnlyList<TimesheetDetail> TimesheetsFor(string projectId)
    {
        if (timesheetsRequested.Add(projectId)) _ = LoadAsync(() => timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None), timesheetsRequested, projectId);
        return timesheetsReadModel.Current(projectId);
    }

    public Task RefreshTimesheetsAsync(string projectId) => timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);

    public IReadOnlyList<SiteAttendance> AttendanceFor(string projectId)
    {
        if (attendanceRequested.Add(projectId)) _ = LoadAsync(() => attendanceReadModel.RefreshAsync(projectId, CancellationToken.None), attendanceRequested, projectId);
        return attendanceReadModel.Current(projectId);
    }

    public Task RefreshAttendanceAsync(string projectId) => attendanceReadModel.RefreshAsync(projectId, CancellationToken.None);

    public async Task<TimesheetDetail> AddWorkerTimesheetAsync(string projectId, string workerId, DateTimeOffset workedOn, decimal hours, string costCode)
    {
        var added = await commands.SendAsync(new AddWorkerTimesheet(projectId, workerId, workedOn, hours, costCode), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return added;
    }

    public async Task<TimesheetDetail> AdjustTimesheetAsync(string projectId, string timesheetId, decimal hours, string costCode)
    {
        var adjusted = await commands.SendAsync(new AdjustTimesheet(timesheetId, hours, costCode), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return adjusted;
    }

    public async Task<LabourApprovalResult> ApproveTimesheetsAsync(string projectId, IReadOnlyList<string> timesheetIds)
    {
        var result = await commands.SendAsync(new ApproveTimesheets(projectId, timesheetIds), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<TimesheetDetail> RejectTimesheetAsync(string projectId, string timesheetId, string reason)
    {
        var rejected = await commands.SendAsync(new RejectTimesheet(timesheetId, reason), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return rejected;
    }

    public IReadOnlyList<LabourSettlementRow> SettlementFor(string projectId)
    {
        if (settlementRequested.Add(projectId)) _ = LoadAsync(() => settlementReadModel.RefreshAsync(projectId, CancellationToken.None), settlementRequested, projectId);
        return settlementReadModel.Current(projectId);
    }

    public Task RefreshSettlementAsync(string projectId) => settlementReadModel.RefreshAsync(projectId, CancellationToken.None);

    public async Task SetTimesheetCoverAsync(string projectId, string xeroLedgerLineId, bool isCovered, string subcontractorId, DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        await commands.SendAsync(new SetXeroLineTimesheetCover(xeroLedgerLineId, isCovered, projectId, subcontractorId, periodStart, periodEnd), CancellationToken.None);
        await settlementReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    public async Task AddSettlementVarianceAsync(string projectId, string costCode, string subcontractorId, decimal amount, string reason, string? xeroLedgerLineId)
    {
        await commands.SendAsync(new AddLabourSettlementVariance(projectId, costCode, subcontractorId, amount, reason, xeroLedgerLineId), CancellationToken.None);
        await settlementReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    private static async Task LoadAsync(Func<Task> refresh, HashSet<string> requested, string projectId)
    {
        try { await refresh(); }
        catch { requested.Remove(projectId); }
    }
}
