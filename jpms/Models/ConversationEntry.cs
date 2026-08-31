namespace Jewel.JPMS.Models;

/// <summary>
/// One message in a threaded conversation, shaped for rendering: who said what, when, whether it
/// is internal-only, and the replies nested under it. Built from RequestMessage or
/// VariationOrderMessage rows via Tree(), which turns the flat ParentMessageId list into the
/// nested shape the conversation components draw.
/// </summary>
public sealed record ConversationEntry(
    string MessageId,
    string? ParentMessageId,
    string AuthorEmail,
    string AuthorName,
    string Body,
    bool IsInternal,
    DateTimeOffset PostedAt)
{
    public IReadOnlyList<ConversationEntry> Replies { get; init; } = Array.Empty<ConversationEntry>();

    /// <summary>
    /// Threads a flat list: top-level messages newest first (the latest exchange is what people
    /// come to check), replies oldest first under their parent (a thread reads downwards).
    /// A reply whose parent isn't in the list — filtered out, or deleted — surfaces as top-level
    /// rather than disappearing.
    /// </summary>
    public static IReadOnlyList<ConversationEntry> Tree(IEnumerable<ConversationEntry> messages)
    {
        var all = messages.ToList();
        var knownIds = all.Select(entry => entry.MessageId).ToHashSet();
        var byParent = all
            .Where(entry => entry.ParentMessageId is not null && knownIds.Contains(entry.ParentMessageId))
            .ToLookup(entry => entry.ParentMessageId!);

        ConversationEntry WithReplies(ConversationEntry entry) => entry with
        {
            Replies = byParent[entry.MessageId]
                .OrderBy(reply => reply.PostedAt)
                .Select(WithReplies)
                .ToList()
        };

        return all
            .Where(entry => entry.ParentMessageId is null || !knownIds.Contains(entry.ParentMessageId))
            .OrderByDescending(entry => entry.PostedAt)
            .Select(WithReplies)
            .ToList();
    }
}
