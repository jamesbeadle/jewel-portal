using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// "Could there have been a response I have not seen?" (2026-08-25). The record page reads its
// mail by tag; thread tagging never sweeps a later reply; so the unfiled-replies read follows the
// tagged conversations and picks the newer, untagged members. These pin the selection rule.
public sealed class UnfiledRepliesTests
{
    private const string Tag = "JPMS/TODO-0083";
    private static readonly DateTimeOffset Day19 = new(2026, 8, 19, 15, 9, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day24 = new(2026, 8, 24, 13, 42, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day25 = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConversationsOf_watermarksEachThreadAtItsNewestTaggedEmail()
    {
        var tagged = new[]
        {
            Email("a", "conv-1", Day19, Tag),
            Email("b", "conv-1", Day24, Tag),
            Email("c", "conv-2", Day19, Tag),
            Email("d", "", Day25, Tag),          // no conversation id — nothing to follow
        };

        var conversations = UnfiledReplies.ConversationsOf(tagged);

        Assert.Equal(2, conversations.Count);
        Assert.Equal("conv-1", conversations[0].ConversationId);
        Assert.Equal(Day24, conversations[0].NewestTaggedAt);
        Assert.Equal("conv-2", conversations[1].ConversationId);
    }

    [Fact]
    public void Select_returnsOnlyNewerUntaggedMembers()
    {
        var thread = new[]
        {
            Email("old-untagged", "conv-1", Day19.AddHours(-1)),   // older: the triager chose to leave it
            Email("tagged", "conv-1", Day24, Tag),
            Email("reply", "conv-1", Day25),                       // the answer nobody has filed
            Email("filed-reply", "conv-1", Day25.AddHours(1), Tag),
            Email("binned", "conv-1", Day25.AddHours(2), "JPMS/Discarded"),
        };

        var unfiled = UnfiledReplies.Select(thread, Tag, Day24);

        var only = Assert.Single(unfiled);
        Assert.Equal("reply", only.Id);
    }

    [Fact]
    public void Select_matchesTheTagCaseInsensitively()
    {
        var thread = new[] { Email("reply", "conv-1", Day25, "jpms/todo-0083") };

        Assert.Empty(UnfiledReplies.Select(thread, Tag, Day24));
    }

    private static MailboxMessage Email(string id, string conversationId, DateTimeOffset receivedAt, params string[] tags) =>
        new(id, $"<{id}@plg.uk>", "justine@plg.uk", "Justine Matthews", "Re: Coombe Lane", "", false, receivedAt, tags, conversationId, null);
}
