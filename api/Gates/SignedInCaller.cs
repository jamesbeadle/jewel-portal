using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Who this invocation is running for, remembered by <see cref="SignedInUserResolver"/> the moment
/// it resolves them. It exists so the business's own correspondence is refused at the data, not
/// only at the door: the readers that fetch mail from the projects mailbox and the reads that
/// return a record's internal notes ask it, so a new endpoint that reuses them for an external
/// login still hands over nothing (2026-09-24 — the RFI conversation served tagged mail to an
/// architect because only the record-mail endpoints were gated). An invocation nobody signed into
/// (a timer, the worker's own sweep) resolves no one and is the system's own reading.
/// </summary>
public sealed class SignedInCaller
{
    public SignedInUser? User { get; private set; }

    public void Is(SignedInUser user) => User = user;

    /// <summary>Whether this caller may read the business's correspondence and internal notes:
    /// the internal team (RecordEmailRoles), judged on the roles they hold, and never an external
    /// login — client, architect, subcontractor or site operative.</summary>
    public bool MayReadInternalCorrespondence =>
        User is null || RecordEmailRoles.Readers.IncludesAny(User.Roles);
}
