using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Free-text search across the WHOLE projects mailbox (Graph $search: subjects, bodies, senders and
// attachment names), relevance-ordered. Feeds the record pages' "Find emails" dialog: search, pick,
// tag to the open record via LinkMessageToRecord — the way more context reaches a record (and
// through it, the assistant) without a trip to mailbox triage. Triage-gated, like the link itself:
// searching the mailbox at large is a triage power, not a record-page one.
public sealed record SearchMailboxMessages(
    string Query,
    int Take = 25) : IQuery<IReadOnlyList<MailboxMessage>>;
