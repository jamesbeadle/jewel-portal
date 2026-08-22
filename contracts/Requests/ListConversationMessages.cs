using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Every Inbox message in one email thread (Graph conversation), read live, oldest first — queue
// members, discarded ones and already-linked ones alike, so the triage detail pane can show an
// email's later replies (they often say how the older messages should be triaged). Not paged: a
// single mail thread is small, so one read returns it whole.
// Subject (optional) is the opened email's own: when the conversation read comes back with only
// that one email — Graph's id having split from the rest of the chain — the members are found by
// matching subject instead, and the page says so (MatchedBySubject).
public sealed record ListConversationMessages(string ConversationId, string? Subject = null) : IQuery<MailboxPage>;
