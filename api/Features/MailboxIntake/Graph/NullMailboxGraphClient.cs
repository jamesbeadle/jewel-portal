using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;
/// <summary>No-op client used when Graph credentials aren't configured: triage shows empty and tag
/// operations report failure (so the UI shows an error rather than a false success).</summary>
public sealed class NullMailboxGraphClient : IMailboxGraphClient
{
    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListDiscardedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListByTagAsync(string tag, string? cursor, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListTaggedAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListByTagsAsync(IReadOnlyList<string> tags, string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct) =>
        Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
    public Task<bool> RemoveTagAsync(string messageId, string? internetMessageId, string tag, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> DiscardAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> RestoreAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> AssignAsync(string messageId, string? internetMessageId, string requestCategory, CancellationToken ct) => Task.FromResult(false);
    public Task<int> ClearRequestTagsAsync(string requestCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<int> RetagAsync(string oldCategory, string newCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<int> AddAliasTagAsync(string existingCategory, string aliasCategory, CancellationToken ct) => Task.FromResult(0);
    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, string? internetMessageId, CancellationToken ct) => Task.FromResult<MailboxSnapshot?>(null);
    public Task<IReadOnlyList<string>> ListUntaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<IReadOnlyList<string>> ListTaggedIdsInConversationAsync(string conversationId, string category, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<int> TagConversationMembersAsync(string conversationId, string category, CancellationToken ct, DateTimeOffset? receivedOnOrBefore = null) =>
        Task.FromResult(0);
    public Task<int> UntagConversationMembersAsync(string conversationId, string category, CancellationToken ct) =>
        Task.FromResult(0);
    public Task<MailboxDraft?> CreateDraftAsync(MailboxDraftMessage draft, CancellationToken ct) =>
        Task.FromResult<MailboxDraft?>(null);
    public Task<MailboxReplyDraft?> CreateReplyDraftAsync(MailboxReplyDraftMessage reply, CancellationToken ct) =>
        Task.FromResult<MailboxReplyDraft?>(null);
    public Task<bool> UpdateDraftEnvelopeAsync(string draftMessageId, IReadOnlyList<MailboxDraftRecipient> to,
        IReadOnlyList<MailboxDraftRecipient> cc, IReadOnlyList<MailboxDraftRecipient> bcc, string subject, CancellationToken ct) =>
        Task.FromResult(false);
    public Task<bool> SendDraftAsync(string draftMessageId, CancellationToken ct) => Task.FromResult(false);
    public Task<string?> GetWebLinkAsync(string messageId, CancellationToken ct) => Task.FromResult<string?>(null);
    public Task<MailboxDraftDeletion> DeleteDraftAsync(string draftMessageId, CancellationToken ct) =>
        Task.FromResult(new MailboxDraftDeletion(MailboxDraftDeleteOutcome.Failed));
}

