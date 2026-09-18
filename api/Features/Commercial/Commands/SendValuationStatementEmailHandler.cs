using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Emails a locked valuation's statement to the project's client side from the shared projects
/// mailbox. The statement is the only client-facing form of the report, so the thread is born on
/// the Client pathway and files itself under the valuation (its JPMS/VAL-… tag); the PDF comes
/// from the shared builder, so the attachment is byte-for-byte what the download endpoint streams.
///
/// The message is <see cref="ValuationStatementEmailComposer"/>'s, shared with the preview, so what
/// a person reads before pressing Send is what leaves. Staging, sending, the degrade to a draft and
/// the audit row are the dispatcher's.
/// </summary>
public sealed class SendValuationStatementEmailHandler
    : ICommandHandler<SendValuationStatementEmail, ValuationStatementEmailOutcome>
{
    private const string StagingRefused =
        "The valuation email couldn't be staged in the shared mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly ValuationStatementEmailComposer composer;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendValuationStatementEmailHandler(
        ValuationStatementEmailComposer composer, OutboundEmailDispatcher dispatcher)
    {
        this.composer = composer;
        this.dispatcher = dispatcher;
    }

    public async Task<ValuationStatementEmailOutcome> HandleAsync(
        SendValuationStatementEmail command, CancellationToken cancellationToken)
    {
        var composed = await composer.ComposeAsync(
            new RecordEmailDraft(
                command.ValuationClaimId, Subject: command.Subject, BodyHtml: command.HtmlBody),
            cancellationToken);

        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Client),
            StagingRefused,
            composed.ProjectId,
            RecordType.ValuationClaim,
            command.ValuationClaimId,
            composed.Reference);

        var dispatch = await dispatcher.DispatchAsync(
            composed.Message, filing, command.SaveAsDraftOnly, cancellationToken);

        return new ValuationStatementEmailOutcome(
            command.ValuationClaimId,
            composed.Reference,
            composed.Message.Subject,
            composed.Message.To.Select(recipient => recipient.Email).ToList(),
            dispatch.WebLink,
            dispatch.MessageId,
            dispatch.Sent,
            dispatch.FailureNote);
    }
}
