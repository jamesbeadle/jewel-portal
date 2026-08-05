using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Projects;

/// <summary>
/// Permanently deletes a project and every record filed under it — requests, variations,
/// valuations, programme, drawings register, financial records, the lot. ConfirmName must match
/// the project's name (the modal makes the user type it); the server re-checks it so a stale or
/// hand-built request cannot delete the wrong project. Xero ledger lines are NOT deleted — they
/// are Xero's facts — their allocation to this project is cleared instead, returning them to the
/// unallocated queue. The audit trail is kept, with a ProjectDeleted event appended.
/// </summary>
public sealed record DeleteProject(
    string ProjectId,
    string ConfirmName) : ICommand<Acknowledgement>;
