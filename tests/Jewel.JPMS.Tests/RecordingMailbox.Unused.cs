using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Tests;

// The reads and the tagging machinery, which no outbound test has any business calling.
public sealed partial class RecordingMailbox
{
    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxPage> ListTaggedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, bool newestFirst, CancellationToken ct) => throw new NotSupportedException();
    public Task<bool> RemoveTagAsync(string messageId, string? internetMessageId, string tag, CancellationToken ct) => throw new NotSupportedException();
    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) => throw new NotSupportedException();
    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) => throw new NotSupportedException();
    public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) => throw new NotSupportedException();
    public Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct) => throw new NotSupportedException();
    public Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct) => throw new NotSupportedException();
    public Task<int> AddAliasTagAsync(string existingCategory, string aliasCategory, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<string>> ListUntaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) => throw new NotSupportedException();
    public Task<IReadOnlyList<string>> ListTaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct) => throw new NotSupportedException();
    public Task<int> TagConversationMembersAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) => throw new NotSupportedException();
    public Task<int> UntagConversationMembersAsync(string conversationId, string category, CancellationToken ct) => throw new NotSupportedException();
    public Task<bool> UpdateDraftEnvelopeAsync(string draftMessageId, IReadOnlyList<MailboxDraftRecipient> to, IReadOnlyList<MailboxDraftRecipient> cc, IReadOnlyList<MailboxDraftRecipient> bcc, string subject, CancellationToken ct) => throw new NotSupportedException();
    public Task<MailboxDraftDeletion> DeleteDraftAsync(string draftMessageId, CancellationToken ct) => throw new NotSupportedException();
}
