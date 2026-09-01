using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// The pure half of ListUnfiledReplies: which conversations to follow, and which of a thread's
// members count as an unfiled reply. Kept free of Graph so the rule can be tested on plain lists.
public static class UnfiledReplies
{
    public sealed record TaggedConversation(string ConversationId, DateTimeOffset NewestTaggedAt);

    // One entry per conversation the tagged mail spans, with the newest tagged email's time — the
    // watermark a reply must be newer than to count as unseen.
    public static IReadOnlyList<TaggedConversation> ConversationsOf(IEnumerable<MailboxMessage> tagged) =>
        tagged
            .Where(email => !string.IsNullOrEmpty(email.ConversationId))
            .GroupBy(email => email.ConversationId)
            .Select(group => new TaggedConversation(group.Key, group.Max(email => email.ReceivedAt)))
            .OrderByDescending(conversation => conversation.NewestTaggedAt)
            .ToList();

    // A member is an unfiled reply when it arrived after the watermark, doesn't carry the record's
    // tag, and hasn't been discarded. Anything older is the thread the triager already decided on.
    public static IReadOnlyList<MailboxMessage> Select(IEnumerable<MailboxMessage> threadMembers, string recordTag, DateTimeOffset newestTaggedAt) =>
        threadMembers
            .Where(email => email.ReceivedAt > newestTaggedAt)
            .Where(email => !email.Categories.Contains(recordTag, StringComparer.OrdinalIgnoreCase))
            .Where(email => !email.Categories.Contains(TriageCategories.Discarded, StringComparer.OrdinalIgnoreCase))
            .ToList();
}
