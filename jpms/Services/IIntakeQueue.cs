using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// Frontend access to live-read mailbox triage. The mailbox is the single source of truth: the queue
// is the Inbox read live, the discarded pile is the "General" folder read live, and every action is
// a single mailbox move (discard, restore, assign to a request, or create a request).
public interface IIntakeQueue
{
    Task<MailboxPage> ListInboxLiveAsync(string? cursor = null, int take = 25, CancellationToken cancellationToken = default);
    Task<MailboxPage> ListDiscardedLiveAsync(string? cursor = null, int take = 25, CancellationToken cancellationToken = default);
    Task<MailboxPage> ListTaggedLiveAsync(string? cursor = null, int take = 25, IReadOnlyList<string>? tags = null, CancellationToken cancellationToken = default);
    Task<MailboxMessageDetail> GetMessageDetailAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<Acknowledgement> DiscardMessageAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<Acknowledgement> RestoreMessageAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<Acknowledgement> RemoveTagFromMessageAsync(string messageId, string? internetMessageId, string tag, CancellationToken cancellationToken = default);
    Task<Acknowledgement> AssignMessageAsync(string messageId, string? internetMessageId, string requestId, CancellationToken cancellationToken = default);
    Task<Request> CreateRequestFromMessageAsync(CreateRequestFromMessage command, CancellationToken cancellationToken = default);
}
