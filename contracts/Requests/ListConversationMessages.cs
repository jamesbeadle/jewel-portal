using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Every Inbox message in one email thread (Graph conversation), read live, oldest first — queue
// members, discarded ones and already-linked ones alike, so the triage detail pane can show an
// email's later replies (they often say how the older messages should be triaged). Not paged: a
// single mail thread is small, so one read returns it whole.
public sealed record ListConversationMessages(string ConversationId) : IQuery<MailboxPage>;
