using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The triage queue, read live from the Inbox. In the live-read model the Inbox *is* the queue:
// anything still sitting in it is un-triaged, because the act of linking/discarding moves the
// message out. Paged server-side so the screen mirrors the Inbox without loading it all.
public sealed record ListInboxMessages(int Skip = 0, int Take = 25) : IQuery<PagedResult<MailboxMessage>>;
