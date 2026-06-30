using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Every tagged email, read live from the Inbox: messages carrying the JPMS marker (i.e. associated
// with at least one workflow). The management surface for the Tagged tab, where tags are added or
// removed. When Tags is non-empty, the view is narrowed to emails carrying ANY of those workflow tags
// (e.g. "JPMS/RFI-001", "JPMS/Discarded") — an OR filter, server-side, so it stays fast as the mailbox
// grows. Cursor-paged.
public sealed record ListTaggedMessages(
    string? Cursor = null,
    int Take = 25,
    IReadOnlyList<string>? Tags = null) : IQuery<MailboxPage>;
