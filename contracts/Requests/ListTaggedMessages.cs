using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Every tagged email, read live from the Inbox: messages carrying the JPMS marker (i.e. associated
// with at least one workflow). The management surface for the Tagged tab, where tags are added or
// removed. Cursor-paged like the queue and discarded views.
public sealed record ListTaggedMessages(string? Cursor = null, int Take = 25) : IQuery<MailboxPage>;
