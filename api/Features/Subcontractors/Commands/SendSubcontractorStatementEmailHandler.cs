using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Emails the statement of account to the subcontractor from the shared projects mailbox, the
/// statement rendered from the live register and attached as a PDF so its figures match the
/// register at the moment it goes. Statement mail is subcontractor correspondence, so the thread is
/// born on that pathway.
///
/// The message is <see cref="SubcontractorStatementEmailComposer"/>'s, shared with the preview;
/// staging, sending, the degrade to a draft and the audit row are the dispatcher's.
/// </summary>
public sealed class SendSubcontractorStatementEmailHandler
    : ICommandHandler<SendSubcontractorStatementEmail, SubcontractorStatementEmailOutcome>
{
    private const string StagingRefused =
        "The statement email couldn't be staged in the shared mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly SubcontractorStatementEmailComposer composer;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendSubcontractorStatementEmailHandler(
        SubcontractorStatementEmailComposer composer, OutboundEmailDispatcher dispatcher)
    {
        this.composer = composer;
        this.dispatcher = dispatcher;
    }

    public async Task<SubcontractorStatementEmailOutcome> HandleAsync(
        SendSubcontractorStatementEmail command, CancellationToken cancellationToken)
    {
        var composed = await composer.ComposeAsync(
            new RecordEmailDraft(
                command.SubcontractorId, Subject: command.Subject, BodyHtml: command.HtmlBody),
            cancellationToken);

        // A statement belongs to a company, not to a record with a tag of its own, so the audit row
        // carries the pathway and the company's name and no record.
        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Subcontractor),
            StagingRefused,
            RecordReference: composed.Reference);

        var dispatch = await dispatcher.DispatchAsync(
            composed.Message, filing, command.SaveAsDraftOnly, cancellationToken);

        return new SubcontractorStatementEmailOutcome(
            command.SubcontractorId,
            composed.Reference,
            composed.Message.Subject,
            composed.Message.To[0].Email,
            dispatch.WebLink,
            dispatch.Sent,
            dispatch.FailureNote);
    }
}
