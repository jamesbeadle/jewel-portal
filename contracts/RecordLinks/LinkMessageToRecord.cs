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
    string?    InternetMessageId = null) : ICommand<Acknowledgement>;
