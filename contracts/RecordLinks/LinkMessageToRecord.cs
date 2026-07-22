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
    bool       AllowCrossPathway = false) : ICommand<Acknowledgement>;
