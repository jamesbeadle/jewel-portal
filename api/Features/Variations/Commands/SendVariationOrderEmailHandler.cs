using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Emails the variation order's official document to the project's client side from the shared
/// projects mailbox. Until 2026-09-19 there was no door: VariationDocumentModel.EmailSubject had
/// been written and nothing called it, so the VO PDF went out by hand — no audit row, no tag on
/// the sent copy, and the client's reply landing in triage rather than on the variation.
///
/// The message is <see cref="VariationOrderEmailComposer"/>'s, shared with the preview; staging,
/// the send, the degrade and the audit row are the dispatcher's.
/// </summary>
public sealed class SendVariationOrderEmailHandler
    : ICommandHandler<SendVariationOrderEmail, VariationOrderEmailOutcome>
{
    private const string StagingRefused =
        "The variation order email couldn't be staged in the projects mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly VariationOrderEmailComposer composer;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendVariationOrderEmailHandler(
        VariationOrderEmailComposer composer, OutboundEmailDispatcher dispatcher)
    {
        this.composer = composer;
        this.dispatcher = dispatcher;
    }

    public async Task<VariationOrderEmailOutcome> HandleAsync(
        SendVariationOrderEmail command, CancellationToken cancellationToken)
    {
        var composed = await composer.ComposeAsync(
            new RecordEmailDraft(command.VariationOrderId, command.RecipientOverride), cancellationToken);

        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Client),
            StagingRefused,
            composed.ProjectId,
            RecordType.Variation,
            command.VariationOrderId,
            composed.Reference);

        var dispatch = await dispatcher.DispatchAsync(
            composed.Message, filing, command.SaveAsDraftOnly, cancellationToken);

        return new VariationOrderEmailOutcome(
            command.VariationOrderId,
            composed.Reference,
            composed.Message.Subject,
            composed.Message.To.Select(recipient => recipient.Email).ToList(),
            dispatch.WebLink,
            DraftMessageId: dispatch.MessageId,
            Sent: dispatch.Sent,
            FailureNote: dispatch.FailureNote);
    }
}
