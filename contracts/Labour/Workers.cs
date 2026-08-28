using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>Whole registry; rates included (endpoint is gated to the commercial team).</summary>
public sealed record ListWorkers : IQuery<IReadOnlyList<Worker>>;

/// <summary>Only a name and an hourly rate are needed. ContactEmail exists solely to link a
/// worker's own portal sign-in (My day) later — it creates no account and emails nothing —
/// so it is optional (2026-08-28, for the connector's add_worker schema; the handler already
/// coalesced null to ""). jpms passes every argument positionally, so the defaults are inert
/// over HTTP.</summary>
public sealed record AddWorker(
    string Name,
    decimal HourlyRate,
    string? SubcontractorId,
    string? ContactEmail = null,
    string? ContactPhone = null) : ICommand<Worker>;

/// <summary>A rate change appends to the worker's rate history (effective now); approved
/// historic timesheets keep their snapshotted rate.</summary>
public sealed record UpdateWorker(
    string WorkerId,
    string Name,
    decimal HourlyRate,
    bool IsActive,
    string? SubcontractorId,
    string ContactEmail,
    string ContactPhone) : ICommand<Worker>;

public sealed record ListWorkerAssignmentsForProject(string ProjectId)
    : IQuery<IReadOnlyList<ProjectWorkerAssignment>>;

/// <summary>Assign (IsActive true) or remove (false) a worker from a project's sign-in sheet.</summary>
public sealed record SetProjectWorkerAssignment(string ProjectId, string WorkerId, bool IsActive)
    : ICommand<ProjectWorkerAssignment>;

/// <summary>Deletes a worker with no timesheet or attendance history. Workers with history
/// can only be deactivated — their record backs recorded cost.</summary>
public sealed record DeleteWorker(string WorkerId) : ICommand<Acknowledgement>;

/// <summary>The daily site register — who was on site, signed in/out when.</summary>
public sealed record ListSiteAttendanceForProject(string ProjectId)
    : IQuery<IReadOnlyList<SiteAttendance>>;
