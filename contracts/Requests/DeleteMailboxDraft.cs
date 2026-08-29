using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Deletes ONE unsent draft from the shared projects mailbox's Drafts folder — the undo for the
/// draft-staging commands (request document and reply drafts, work-order PO emails, tender
/// invites, valuation report emails) when a draft was staged in error or superseded.
/// <see cref="MessageId"/> is the draft's mailbox message id, as the draft-staging results return
/// it (DraftMessageId) and the audit trail's DraftCreated rows carry it. The mailbox client
/// verifies the message really is an unsent draft before deleting, so a sent or received email can
/// never be removed by this command. Graph moves the deleted draft to Deleted Items rather than
/// wiping it, so a person can still recover a mistaken delete from Outlook for a while.
/// </summary>
public sealed record DeleteMailboxDraft(string MessageId) : ICommand<Acknowledgement>;
