using System.Collections.Concurrent;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// Keeps a record's tag spanning the WHOLE email thread, not just the one message a human happened to
// click. Emails are linked by conversation (reply/forward → same Graph conversationId), so when an
// email is tagged to a record we also tag every other mailbox message in that conversation — Inbox
// members AND the mailbox's own sent replies (Sent Items), which never arrive back in the Inbox. The
// record then reads the full thread back through the one tag — the entire context, both directions.
//
// Three entry points:
//   • TagThreadAsync      — at link time: tag the anchor (verified) + its conversation siblings.
//   • SyncThreadsAsync    — catch-up for one record: re-scan its threads and tag replies that arrived later.
//   • SweepQueuePageAsync — catch-up for the queue: a queued email whose conversation already carries a
//                           record's tag inherits it, so replies to triaged threads never linger in triage.
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

    // Re-tag any mailbox message that belongs to a conversation this record already touches but isn't
    // tagged yet — i.e. replies that arrived after the original link, and replies the mailbox itself
    // sent outside the portal (they sit in Sent Items, so an inbox sweep would never see them). Cheap
    // when nothing is new (the per-conversation query returns no untagged members, so no writes
    // happen). Returns the count tagged.
    public async Task<int> SyncThreadsAsync(string category, CancellationToken ct)
    {
        var conversationIds = await graph.ListConversationIdsByCategoryAsync(category, ct);
        var tagged = 0;
        foreach (var conversationId in conversationIds)
            tagged += await TagConversationSiblingsAsync(conversationId, category, ct);
        return tagged;
    }

    // Queue hygiene, run when the triage queue is listed: any queued (untagged) email whose
    // conversation already carries a record's workflow tag — because the thread was triaged earlier
    // and this is a new reply, or because the mailbox's own sent reply carries the tag — inherits
    // that tag, so it leaves the queue instead of waiting for a manual per-record sync. Discarded
    // is deliberately NOT inherited: a fresh reply to a discarded thread may be a real request, so
    // it stays for a human to look at. Best-effort throughout: a conversation that can't be read or
    // tagged is simply left queued for this render and retried on the next listing.
    // Returns the conversation ids that adopted a tag, so the caller can drop their messages from
    // the page it is about to return (the category-filtered re-read may lag the verified writes).
    public async Task<IReadOnlySet<string>> SweepQueuePageAsync(
        IReadOnlyList<MailboxMessage> queuePage, CancellationToken ct)
    {
        var swept = new HashSet<string>(StringComparer.Ordinal);
        foreach (var conversationId in queuePage
            .Select(m => m.ConversationId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal))
        {
            // The queue is re-listed after every triage action, so remember conversations recently
            // checked and found tagless and skip re-reading them for a couple of minutes. Entries
            // expire so a thread linked elsewhere in the meantime is still picked up promptly.
            if (recentlyCheckedClean.TryGetValue(conversationId, out var checkedAt)
                && DateTimeOffset.UtcNow - checkedAt < RecheckCleanAfter)
                continue;

            try
            {
                // The whole thread (Inbox + the mailbox's own sent copies; unsent drafts excluded —
                // a staged draft is provisional, not a triage decision). Any workflow tag on any
                // member is a decision the queued members should inherit.
                var thread = await graph.ListConversationAsync(conversationId, ct);
                var tags = thread.Items
                    .SelectMany(m => m.Categories)
                    .Where(c => TriageCategories.IsWorkflowTag(c)
                        && !string.Equals(c, TriageCategories.Discarded, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (tags.Count == 0)
                {
                    recentlyCheckedClean[conversationId] = DateTimeOffset.UtcNow;
                    continue;
                }

                var tagged = 0;
                foreach (var tag in tags)
                    tagged += await TagConversationSiblingsAsync(conversationId, tag, ct);
                if (tagged > 0)
                    swept.Add(conversationId);
                recentlyCheckedClean.TryRemove(conversationId, out _);
            }
            catch { /* best-effort — retried on the next listing */ }
        }

        // Bound the memory: prune expired entries once the map grows past a sensible queue size.
        if (recentlyCheckedClean.Count > 1000)
            foreach (var stale in recentlyCheckedClean
                .Where(kv => DateTimeOffset.UtcNow - kv.Value >= RecheckCleanAfter)
                .Select(kv => kv.Key)
                .ToList())
                recentlyCheckedClean.TryRemove(stale, out _);

        return swept;
    }

    // Conversations recently checked by SweepQueuePageAsync and found to carry no workflow tag,
    // with when they were checked. Static: the check is a pure mailbox read, so one memory serves
    // every scoped instance on this app instance.
    private static readonly ConcurrentDictionary<string, DateTimeOffset> recentlyCheckedClean = new(StringComparer.Ordinal);
    private static readonly TimeSpan RecheckCleanAfter = TimeSpan.FromMinutes(2);

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
