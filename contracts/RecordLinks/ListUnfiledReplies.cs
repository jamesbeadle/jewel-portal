using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// "Could there have been a response I have not seen?" — the emails that arrived on one of the
// record's threads AFTER the last email filed to it, and are not yet tagged to the record. Thread
// tagging deliberately never sweeps a reply that arrives after the triage decision (it queues in
// the Control Centre for its own decision), so a record page reading only its tag can be blind
// to the very reply the assignee is waiting for. This read closes that gap: it follows each
// tagged email's conversation live and returns the newer, untagged members, newest first, so the
// page can say "2 newer replies on this thread aren't filed here" and offer to file them.
// Discarded emails and unsent drafts are never returned. Nothing is stored or changed.
public sealed record ListUnfiledReplies(
    RecordType Type,
    string RecordId) : IQuery<IReadOnlyList<MailboxMessage>>;
