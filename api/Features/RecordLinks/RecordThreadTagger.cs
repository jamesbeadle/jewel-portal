using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// Keeps a record's tag spanning the WHOLE email thread, not just the one message a human happened to
// click. Emails are linked by conversation (reply/forward → same Graph conversationId), so when an
// email is tagged to a record we also tag every other Inbox message in that conversation. The record
// then reads the full thread back through the one tag — the entire context, not a single message.
//
// Two entry points:
//   • TagThreadAsync   — at link time: tag the anchor (verified) + its conversation siblings.
//   • SyncThreadsAsync — catch-up: re-scan the record's threads and tag replies that arrived later.
// Sibling tagging is best-effort (the anchor is the real association); a sibling that can't be tagged
// is logged and skipped rather than failing the link.
public sealed class RecordThreadTagger
{
    private readonly IMailboxGraphClient graph;

    public RecordThreadTagger(IMailboxGraphClient graph) { this.graph = graph; }

    // Tag the anchor message (verified by read-back — this is the association the caller depends on),
    // then tag every other Inbox message in the same conversation. Returns false only if the anchor
    // itself couldn't be tagged; sibling failures don't fail the operation.
    public async Task<bool> TagThreadAsync(
        string anchorMessageId, string? internetMessageId, string? conversationId, string category, CancellationToken ct)
    {
        var anchored = await graph.AssignAsync(anchorMessageId, internetMessageId, category, ct);
        if (!anchored)
            return false;

        await TagConversationSiblingsAsync(conversationId, category, ct);
        return true;
    }

    // Re-tag any Inbox message that belongs to a conversation this record already touches but isn't
    // tagged yet — i.e. replies that arrived after the original link. Cheap when nothing is new (the
    // per-conversation query returns no untagged members, so no writes happen). Returns the count tagged.
    public async Task<int> SyncThreadsAsync(string category, CancellationToken ct)
    {
        var conversationIds = await graph.ListInboxConversationIdsByCategoryAsync(category, ct);
        var tagged = 0;
        foreach (var conversationId in conversationIds)
            tagged += await TagConversationSiblingsAsync(conversationId, category, ct);
        return tagged;
    }

    private async Task<int> TagConversationSiblingsAsync(string? conversationId, string category, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return 0;

        var ids = await graph.ListUntaggedInboxIdsInConversationAsync(conversationId, category, ct);
        var tagged = 0;
        foreach (var id in ids)
            if (await graph.AssignAsync(id, null, category, ct))
                tagged++;
        return tagged;
    }
}
