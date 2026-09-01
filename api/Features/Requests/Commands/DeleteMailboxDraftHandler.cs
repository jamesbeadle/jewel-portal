using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Deletes one unsent draft from the shared mailbox's Drafts folder — the undo for the draft-
/// staging handlers. The Graph client refuses anything that is not an unsent draft (read-back
/// verified before the DELETE), so the guards here only translate its outcomes into user-facing
/// answers. The deletion lands in the audit trail as the inverse of DraftCreated: the draft itself
/// is gone (recoverable from Outlook's Deleted Items for a while), so the row is the surviving
/// record of what was staged and then withdrawn — mirroring DraftWorkOrderDeleted.
/// </summary>
public sealed class DeleteMailboxDraftHandler : ICommandHandler<DeleteMailboxDraft, Acknowledgement>
{
    private readonly IMailboxGraphClient graph;
    private readonly AuditTrail audit;

    public DeleteMailboxDraftHandler(IMailboxGraphClient graph, AuditTrail audit)
    { this.graph = graph; this.audit = audit; }

    public async Task<Acknowledgement> HandleAsync(DeleteMailboxDraft command, CancellationToken cancellationToken)
    {
        var deletion = await graph.DeleteDraftAsync(command.MessageId, cancellationToken);
        switch (deletion.Outcome)
        {
            case MailboxDraftDeleteOutcome.NotADraft:
                throw new InvalidOperationException(
                    "That message is not an unsent draft — only drafts still waiting in the Drafts "
                    + "folder can be deleted, never sent or received mail."
                    + (string.IsNullOrEmpty(deletion.Subject) ? "" : $" (Subject: \"{deletion.Subject}\".)"));
            case MailboxDraftDeleteOutcome.NotFound:
                throw new InvalidOperationException(
                    "No message with that id is in the mailbox — the draft may already have been "
                    + "deleted, or it was sent and the id has moved on with the sent copy.");
            case MailboxDraftDeleteOutcome.Failed:
                throw new InvalidOperationException(
                    "The draft couldn't be deleted — the mailbox connection may have failed. Check "
                    + "and try again.");
        }

        // Audit AFTER the delete stuck (same rule as DraftCreated: a failed call records nothing).
        // No webLink — the draft it would open is gone; the subject names what was withdrawn.
        await audit.WriteAsync(
            AuditEventType.MailboxDraftDeleted,
            string.IsNullOrEmpty(deletion.Subject)
                ? "Mailbox draft deleted before sending."
                : $"Mailbox draft \"{deletion.Subject}\" deleted before sending.",
            emailMessageId: command.MessageId,
            cancellationToken: cancellationToken);

        return new Acknowledgement(command.MessageId);
    }
}
