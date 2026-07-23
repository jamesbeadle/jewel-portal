using System.Collections.Concurrent;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// Spreads a record's tag across the email thread AS IT EXISTS at the moment a human triages it — the
// one deliberate decision covers the messages already sitting in the mailbox (Inbox members AND the
// mailbox's own sent replies in Sent Items, which never arrive back in the Inbox), so nobody re-triages
// the past. Deliberately NOTHING here tags messages that arrive AFTER that decision: a new reply is a
// fresh piece of correspondence and must land in the triage queue on its own, even when the rest of
// its thread is already linked (the queue shows the thread's existing tags as a hint instead).
//
// Entry points:
//   • TagThreadAsync         — at triage time: tag the anchor (verified) + its current conversation siblings.
//   • UntagThreadAsync       — the inverse, for reversing a thread-wide tag.
//   • LookupThreadTagsAsync  — read-only: the record tags already carried by queued emails' threads,
//                              so the queue can hint "this is a reply to an already-linked thread".
// Sibling tagging is best-effort (the anchor is the real association); a sibling that can't be tagged
// is logged and skipped rather than failing the link.
public sealed class RecordThreadTagger
{
    private readonly IMailboxGraphClient graph;

    public RecordThreadTagger(IMailboxGraphClient graph) { this.graph = graph; }

    // Tag the anchor message (verified by read-back — this is the association the caller depends on),
    // then tag every other mailbox message in the same conversation. Returns false only if the anchor
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

    // Read-only queue annotation, run when the triage queue is listed: for each queued email's
    // conversation, the record tags its thread ALREADY carries (because an older member was triaged
    // earlier, or the mailbox's own sent copy was tagged). Writes nothing — a new reply to a linked
    // thread stays in the queue for its own triage decision; the tags returned here let the UI hint
    // "reply to REQ-0007" so re-linking is one step. Discarded and pathway (bucket) tags are not
    // hints: a fresh reply to a discarded thread may be a real request, and a bucket alone is not a
    // triage decision. Best-effort: a conversation that can't be read simply gets no hint this
    // render. Returns conversationId → distinct record tags, only for threads that have any.
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> LookupThreadTagsAsync(
        IReadOnlyList<MailboxMessage> queuePage, CancellationToken ct)
    {
        var found = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var conversationId in queuePage
            .Select(m => m.ConversationId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal))
        {
            // The queue is re-listed after every triage action, so remember recent per-conversation
            // answers and skip re-reading for a couple of minutes. Entries expire so a thread linked
            // elsewhere in the meantime still gains its hint promptly.
            if (recentlyLookedUp.TryGetValue(conversationId, out var cached)
                && DateTimeOffset.UtcNow - cached.At < RelookupAfter)
            {
                if (cached.Tags.Count > 0)
                    found[conversationId] = cached.Tags;
                continue;
            }

            try
            {
                // The whole thread (Inbox + the mailbox's own sent copies; unsent drafts excluded —
                // a staged draft is provisional, not a triage decision). Any RECORD tag on any
                // member is an existing decision worth surfacing. (MailboxMessage.Categories
                // excludes bucket tags, so this test is on record tags alone.)
                var thread = await graph.ListConversationAsync(conversationId, ct);
                IReadOnlyList<string> tags = thread.Items
                    .SelectMany(m => m.Categories)
                    .Where(c => TriageCategories.IsWorkflowTag(c)
                        && !TriageCategories.IsBucketTag(c)
                        && !string.Equals(c, TriageCategories.Discarded, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                recentlyLookedUp[conversationId] = (DateTimeOffset.UtcNow, tags);
                if (tags.Count > 0)
                    found[conversationId] = tags;
            }
            catch { /* best-effort — no hint this render, retried on the next listing */ }
        }

        // Bound the memory: prune expired entries once the map grows past a sensible queue size.
        if (recentlyLookedUp.Count > 1000)
            foreach (var stale in recentlyLookedUp
                .Where(kv => DateTimeOffset.UtcNow - kv.Value.At >= RelookupAfter)
                .Select(kv => kv.Key)
                .ToList())
                recentlyLookedUp.TryRemove(stale, out _);

        return found;
    }

    // Conversations recently examined by LookupThreadTagsAsync, with what was found and when.
    // Static: the lookup is a pure mailbox read, so one memory serves every scoped instance on
    // this app instance.
    private static readonly ConcurrentDictionary<string, (DateTimeOffset At, IReadOnlyList<string> Tags)> recentlyLookedUp = new(StringComparer.Ordinal);
    private static readonly TimeSpan RelookupAfter = TimeSpan.FromMinutes(2);

    private async Task<int> TagConversationSiblingsAsync(string? conversationId, string category, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return 0;

        var ids = await graph.ListUntaggedIdsInConversationAsync(conversationId, category, ct);
        var tagged = 0;
        foreach (var id in ids)
            if (await graph.AssignAsync(id, null, category, ct))
                tagged++;
        return tagged;
    }

    // The inverse of TagThreadAsync: remove the category from the anchor (verified) and from every
    // other mailbox message in its conversation that still carries it. Used to reverse a thread-wide tag,
    // e.g. restoring a discarded thread. Returns false only if the anchor couldn't be un-tagged.
    public async Task<bool> UntagThreadAsync(
        string anchorMessageId, string? internetMessageId, string? conversationId, string category, CancellationToken ct)
    {
        var anchorCleared = await graph.RemoveTagAsync(anchorMessageId, internetMessageId, category, ct);
        if (!anchorCleared)
            return false;

        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            var ids = await graph.ListTaggedIdsInConversationAsync(conversationId, category, ct);
            foreach (var id in ids)
                await graph.RemoveTagAsync(id, null, category, ct); // best-effort
        }
        return true;
    }
}
