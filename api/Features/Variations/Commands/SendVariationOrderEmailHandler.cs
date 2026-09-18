using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Variations.Documents;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Emails the variation order's official document to the project's client side from the shared
/// projects mailbox. Until 2026-09-19 there was no door: VariationDocumentModel.EmailSubject had
/// been written and nothing called it, so the VO PDF went out by hand — no audit row, no tag on
/// the sent copy, and the client's reply landing in triage rather than on the variation.
///
/// The PDF comes from the shared builder and renderer, so the attachment is byte-for-byte the file
/// the Download button streams. Staging, the send, the degrade and the audit row are the
/// dispatcher's, as for every other record's email.
/// </summary>
public sealed partial class SendVariationOrderEmailHandler
    : ICommandHandler<SendVariationOrderEmail, VariationOrderEmailOutcome>
{
    private const string StagingRefused =
        "The variation order email couldn't be staged in the projects mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendVariationOrderEmailHandler(JpmsContext context, OutboundEmailDispatcher dispatcher)
    {
        this.context = context;
        this.dispatcher = dispatcher;
    }

    public async Task<VariationOrderEmailOutcome> HandleAsync(
        SendVariationOrderEmail command, CancellationToken cancellationToken)
    {
        var variation = await EmailableVariationAsync(command.VariationOrderId, cancellationToken);
        var recipients = await RecipientsForAsync(variation.ProjectId, command.RecipientOverride, cancellationToken);

        var model = await VariationDocumentBuilder.BuildAsync(context, command.VariationOrderId, cancellationToken);
        if (model is null) throw new InvalidOperationException($"Variation '{command.VariationOrderId}' not found.");

        var message = await StagedMessageAsync(variation, model, recipients, cancellationToken);
        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Client),
            StagingRefused,
            variation.ProjectId,
            RecordType.Variation,
            variation.VariationOrderId,
            model.DocumentReference);

        var dispatch = await dispatcher.DispatchAsync(message, filing, command.SaveAsDraftOnly, cancellationToken);
        return OutcomeFor(variation, model, recipients, dispatch);
    }

    private static VariationOrderEmailOutcome OutcomeFor(
        VariationOrderEntity variation, VariationDocumentModel model,
        List<MailboxDraftRecipient> recipients, OutboundEmailDispatch dispatch) =>
        new(variation.VariationOrderId,
            model.DocumentReference,
            model.EmailSubject,
            recipients.Select(recipient => recipient.Email).ToList(),
            dispatch.WebLink,
            DraftMessageId: dispatch.MessageId,
            Sent: dispatch.Sent,
            FailureNote: dispatch.FailureNote);

    /// <summary>EMAIL POLICY: only an Issued, Awaiting AI or Approved variation is a document to
    /// send — a quoting-stage price has not been put, and a rejected one is terminal.</summary>
    private async Task<VariationOrderEntity> EmailableVariationAsync(
        string variationOrderId, CancellationToken cancellationToken)
    {
        var variation = await context.VariationOrders
            .FirstOrDefaultAsync(row => row.VariationOrderId == variationOrderId, cancellationToken);
        if (variation is null) throw new InvalidOperationException($"Variation '{variationOrderId}' not found.");

        var status = (VariationOrderStatus)variation.Status;
        if (status.IsEmailable()) return variation;

        throw new InvalidOperationException(
            $"A {status.DisplayName()} variation is not emailed — issue it to the client first. "
            + "Only an issued, awaiting-AI or approved variation order goes out as a document.");
    }
}
