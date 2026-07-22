using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The triage queue, read live from the Inbox: messages that have NOT been tagged as triaged. Nothing
// is moved — triage just tags the email — so the Inbox stays whole and "untriaged" is a server-side
// category filter. Paged with an opaque cursor (pass the previous page's NextCursor to go forward).
// NewestFirst flips the order: the default (false) reads oldest-first so the backlog clears from
// page one; true reads newest-first for checking what's just arrived.
public sealed record ListInboxMessages(string? Cursor = null, int Take = 25, bool NewestFirst = false) : IQuery<MailboxPage>;
