using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// Frontend access to live-read mailbox triage. The mailbox is the single source of truth: the queue
// is the Inbox read live, the discarded pile is the "General" folder read live, and every action is
// a single mailbox move (discard, restore, link to a record, or create a record).
public interface IIntakeQueue
{
    // newestFirst flips each list's read order; the default (false) reads oldest-first so the
    // backlog clears from page one. The triage page persists the user's last choice.
    Task<MailboxPage> ListInboxLiveAsync(string? cursor = null, int take = 25, bool newestFirst = false, CancellationToken cancellationToken = default);
    Task<MailboxPage> ListDiscardedLiveAsync(string? cursor = null, int take = 25, bool newestFirst = false, CancellationToken cancellationToken = default);
    Task<MailboxPage> ListTaggedLiveAsync(string? cursor = null, int take = 25, IReadOnlyList<string>? tags = null, bool newestFirst = false, CancellationToken cancellationToken = default);

    // An email's whole thread: every Inbox message sharing its Graph conversation id, oldest first,
    // regardless of tags. Backs the triage detail pane's thread panel — later replies often say how
    // the older messages should be triaged, and link/discard already apply conversation-wide.
    Task<MailboxPage> ListConversationLiveAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<MailboxMessageDetail> GetMessageDetailAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default);

    Task<Acknowledgement> DiscardMessageAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<Acknowledgement> RestoreMessageAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<Acknowledgement> RemoveTagFromMessageAsync(string messageId, string? internetMessageId, string tag, CancellationToken cancellationToken = default);
    Task<Acknowledgement> AssignMessageAsync(string messageId, string? internetMessageId, string requestId, CancellationToken cancellationToken = default);
    Task<Request> CreateRequestFromMessageAsync(CreateRequestFromMessage command, CancellationToken cancellationToken = default);

    // Triage "Reply in thread": the reply written in the portal is staged as an Outlook draft on
    // the email (projects mailbox, whole thread quoted behind it) and doubles as the description of
    // a General request created from the email in the background — replying IS the triage. The
    // outcome carries the created request and the draft's weblink so the triager can open the
    // pre-filled draft in Outlook, review it and send it themselves.
    Task<ReplyInThreadOutcome> ReplyInThreadFromMessageAsync(ReplyInThreadFromMessage command, CancellationToken cancellationToken = default);

    // Record-agnostic linking: list the records of a type on a project (for the category-first picker),
    // and link a message to one. The link tags "JPMS/<ref>" identically for every record type, and the
    // record reads its mail back live by that tag. AssignMessageAsync is the Request-only special case.
    Task<IReadOnlyList<LinkableRecord>> ListLinkableRecordsAsync(string projectId, RecordType type, CancellationToken cancellationToken = default);
    // Pathway (docs/Pathway-Split-Platform-Flow-Plan.md §2.3): pathway names the triager's explicit
    // choice ("Client"/"Subcontractor"/"Internal") for pathway-neutral record types (cost centres);
    // allowCrossPathway is the explicit consent to a Subcontractor↔Internal dual filing after the
    // UI's warning. The client wall (Client never shares a thread with the others) has no override.
    Task<Acknowledgement> LinkMessageToRecordAsync(string messageId, string? internetMessageId, RecordType type, string recordId, string? pathway = null, bool allowCrossPathway = false, CancellationToken cancellationToken = default);

    // Catch-up: re-tag any Inbox replies that joined a record's threads after it was linked, so the
    // record's tag keeps spanning the whole conversation. Safe to call when a record is opened.
    Task<Acknowledgement> SyncRecordThreadTagsAsync(RecordType type, string recordId, CancellationToken cancellationToken = default);

    // Create a new Bid Package Invite from an email (Draft package + link the email to it). The Request
    // equivalent is CreateRequestFromMessageAsync; both are the "create a new record from this email"
    // half of triage, one per record type.
    Task<BidPackage> CreateBidPackageFromMessageAsync(CreateBidPackageFromMessage command, CancellationToken cancellationToken = default);

    // Create one or more to-do items on a project from an email (several can be captured from a single
    // message). The email is tagged "JPMS/TODO-####" per item, so each item reads its mail back live by
    // its own tag — the to-do half of "create a new record from this email".
    Task<IReadOnlyList<TodoItem>> CreateTodoItemsFromMessageAsync(CreateTodoItemsFromMessage command, CancellationToken cancellationToken = default);
}
