using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The discarded pile, read live from the "General" folder under the Inbox. Discarding moves a
// message here; restoring moves it back to the Inbox. Paged server-side.
public sealed record ListDiscardedMessages(int Skip = 0, int Take = 25) : IQuery<PagedResult<MailboxMessage>>;
