using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// Frontend access to live-read mailbox triage. The mailbox is the single source of truth: the queue
// is the Inbox read live, the discarded pile is the "General" folder read live, and every action is
// a single mailbox move (discard, restore, link to a record, or create a record).
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

    // Record-agnostic linking: list the records of a type on a project (for the category-first picker),
    // and link a message to one. The link tags "JPMS/<ref>" identically for every record type, and the
    // record reads its mail back live by that tag. AssignMessageAsync is the Request-only special case.
    Task<IReadOnlyList<LinkableRecord>> ListLinkableRecordsAsync(string projectId, RecordType type, CancellationToken cancellationToken = default);
    Task<Acknowledgement> LinkMessageToRecordAsync(string messageId, string? internetMessageId, RecordType type, string recordId, CancellationToken cancellationToken = default);

    // Create a new Bid Package Invite from an email (Draft package + link the email to it). The Request
    // equivalent is CreateRequestFromMessageAsync; both are the "create a new record from this email"
    // half of triage, one per record type.
    Task<BidPackage> CreateBidPackageFromMessageAsync(CreateBidPackageFromMessage command, CancellationToken cancellationToken = default);
}
