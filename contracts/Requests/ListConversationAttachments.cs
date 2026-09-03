using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Every attachment anywhere in one email thread, grouped by the message that carried it, read
// live — so a reply can pick up a file from an EARLIER message in the chain (the one being
// replied to is only the latest), without waiting for the file to reach document triage. The
// thread is resolved exactly as ListConversationMessages resolves it (Graph conversation id, then
// the subject fallback when the id has split), then each member that reports attachments is
// asked for its list; inline body images are left out. Oldest message first.
public sealed record ListConversationAttachments(string ConversationId, string? Subject = null)
    : IQuery<IReadOnlyList<ConversationAttachmentGroup>>;
