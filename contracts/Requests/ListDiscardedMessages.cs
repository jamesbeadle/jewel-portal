using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The discarded pile, read live from the Inbox: messages tagged with the "discarded" category.
// Discarding adds the tag, restoring removes it — the email never leaves the Inbox. Cursor-paged.
// NewestFirst flips the order (default oldest-first, matching the queue).
public sealed record ListDiscardedMessages(string? Cursor = null, int Take = 25, bool NewestFirst = false) : IQuery<MailboxPage>;
