using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Labour;

/// <summary>The whole worker registry (not per-project). Commercial-team-only read.</summary>
public sealed class WorkersReadModel
{
    private readonly IQueryClient queries;
    private IReadOnlyList<Worker>? workers;

    public WorkersReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Worker> Current => workers ?? Array.Empty<Worker>();
    /// <summary>True once the registry has landed. Current answers with an empty list until then,
    /// which is indistinguishable from a registry with nobody in it — so anything rendering a
    /// worker list, a picker or a count must gate on this first.</summary>
    public bool IsLoaded => workers is not null;

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
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => assignmentsByProject.ContainsKey(projectId);

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
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => timesheetsByProject.ContainsKey(projectId);

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
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => attendanceByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        attendanceByProject[projectId] = await queries.AskAsync(new ListSiteAttendanceForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>The signed-in worker's own day (My Day page). Single-key cache.</summary>
public sealed class MyLabourDayReadModel
{
    private readonly IQueryClient queries;
    private MyLabourDay? day;

    public MyLabourDayReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public MyLabourDay? Current => day;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        day = await queries.AskAsync(new GetMyLabourDay(), cancellationToken);
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
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => settlementByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        settlementByProject[projectId] = await queries.AskAsync(new ListLabourSettlementForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
