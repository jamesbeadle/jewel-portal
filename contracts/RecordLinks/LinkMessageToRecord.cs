using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Link a mailbox message to an existing record of any type. The handler tags the email
// "JPMS/<record.TagReference>" (verified by read-back) — the tag IS the association, no copy of the
// email is stored. The record reads its emails back live by the same tag (RecordEmailReader).
//
// This is the record-agnostic generalisation of AssignMessageToRequest: the same mechanism, but the
// target is identified by (Type, RecordId) instead of being hardwired to a request. AssignMessage
// ToRequest is kept as a Request-typed adapter over this command during the migration.
public sealed record LinkMessageToRecord(
    string     MessageId,
    RecordType Type,
    string     RecordId,
    string?    InternetMessageId = null,
    // The triager's explicit pathway choice ("Client" / "Subcontractor" / "Internal") for
    // pathway-neutral record types (CostCentre) whose side the record type alone can't imply.
    // Ignored when the record type implies a pathway (a Request is always Client, a bid package
    // always Subcontractor). Null for neutral links (Todo) = no pathway involvement.
    string?    Pathway = null,
    // Explicit consent to file this thread under a second NON-CLIENT pathway (Subcontractor ↔
    // Internal). The client wall has no override: Client never shares a thread with the others,
    // whatever this flag says.
    bool       AllowCrossPathway = false,
    // How far the tags spread across the email's conversation. The default keeps the long-standing
    // behaviour (and is what an omitted property deserialises to): the anchor plus the thread
    // behind it. MessageOnly tags just the clicked email — its thread siblings stay in the triage
    // queue for their own decisions (the Control Centre's Relevant Event tick uses this unless
    // "Triage entire thread" is ticked). EntireThread sweeps the whole conversation as it exists
    // right now, newer replies included.
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor) : ICommand<Acknowledgement>;

/// <summary>How far a record link's tags spread across the email's conversation.</summary>
public enum LinkThreadScope
{
    /// <summary>The anchor email plus every conversation member received at or before it — the
    /// long-standing default: a decision made on an email covers that email and the thread behind
    /// it, never a newer reply.</summary>
    ThreadBehindAnchor = 0,

    /// <summary>The anchor email alone; its conversation siblings keep queueing for their own
    /// triage decisions.</summary>
    MessageOnly = 1,

    /// <summary>Every current member of the conversation, newer replies included — the explicit
    /// "triage the entire thread" choice.</summary>
    EntireThread = 2,
}
