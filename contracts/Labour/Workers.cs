using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>Whole registry; rates included (endpoint is gated to the commercial team).</summary>
public sealed record ListWorkers : IQuery<IReadOnlyList<Worker>>;

public sealed record AddWorker(
    string Name,
    decimal HourlyRate,
    string? SubcontractorId,
    string ContactEmail,
    string ContactPhone) : ICommand<Worker>;

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
