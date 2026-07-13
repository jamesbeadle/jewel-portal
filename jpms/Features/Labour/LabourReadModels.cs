using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Labour;

/// <summary>The whole worker registry (not per-project). Commercial-team-only read.</summary>
public sealed class WorkersReadModel
{
    private readonly IQueryClient queries;
    private IReadOnlyList<Worker> workers = Array.Empty<Worker>();

    public WorkersReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Worker> Current => workers;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        workers = await queries.AskAsync(new ListWorkers(), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class WorkerAssignmentsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ProjectWorkerAssignment>> assignmentsByProject = new();

    public WorkerAssignmentsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ProjectWorkerAssignment> Current(string projectId) =>
        assignmentsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProjectWorkerAssignment>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        assignmentsByProject[projectId] = await queries.AskAsync(new ListWorkerAssignmentsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class LabourTimesheetsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<TimesheetDetail>> timesheetsByProject = new();

    public LabourTimesheetsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<TimesheetDetail> Current(string projectId) =>
        timesheetsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<TimesheetDetail>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        timesheetsByProject[projectId] = await queries.AskAsync(new ListTimesheetDetailsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class SiteAttendanceReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<SiteAttendance>> attendanceByProject = new();

    public SiteAttendanceReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<SiteAttendance> Current(string projectId) =>
        attendanceByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<SiteAttendance>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        attendanceByProject[projectId] = await queries.AskAsync(new ListSiteAttendanceForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class SiteAccessReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, SiteAccess> accessByProject = new();

    public SiteAccessReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public SiteAccess? Current(string projectId) =>
        accessByProject.TryGetValue(projectId, out var access) ? access : null;

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        accessByProject[projectId] = await queries.AskAsync(new GetSiteAccess(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class LabourSettlementReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<LabourSettlementRow>> settlementByProject = new();

    public LabourSettlementReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<LabourSettlementRow> Current(string projectId) =>
        settlementByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<LabourSettlementRow>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        settlementByProject[projectId] = await queries.AskAsync(new ListLabourSettlementForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
