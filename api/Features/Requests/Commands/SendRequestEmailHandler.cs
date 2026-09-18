using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Api.Features.Requests.Recipients;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Emails the request's official document from the projects mailbox as a new thread — recipients,
/// subject, cover note and the freshly rendered PDF all composed here. SaveAsDraftOnly stops after
/// staging, leaving the reviewed draft in Drafts for Outlook, which is what this handler did for
/// everybody until 2026-09-17.
///
/// Staging, the send, the degrade back to a draft and the audit row are the dispatcher's
/// (OutboundEmailDispatcher); the status move is RequestLifecycle's, shared with the reply. The
/// steps either side of it have their own homes: who it goes to (.Recipients), what leaves with it
/// (.Draft and .Attachments) and what it says (.CoverNote).
/// </summary>
public sealed partial class SendRequestEmailHandler : ICommandHandler<SendRequestEmail, RequestEmailOutcome>
{
    private const string StagingRefused =
        "The email couldn't be staged in the projects mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;
    private readonly MailboxIntakeOptions mailboxOptions;
    private readonly Attachments.IRequestAttachmentStore attachmentStore;
    private readonly IEmailFileShareStore shareStore;

    public SendRequestEmailHandler(
        JpmsContext context,
        OutboundEmailDispatcher dispatcher,
        MailboxIntakeOptions mailboxOptions,
        Attachments.IRequestAttachmentStore attachmentStore,
        IEmailFileShareStore shareStore)
    {
        this.context = context;
        this.dispatcher = dispatcher;
        this.mailboxOptions = mailboxOptions;
        this.attachmentStore = attachmentStore;
        this.shareStore = shareStore;
    }

    public async Task<RequestEmailOutcome> HandleAsync(SendRequestEmail command, CancellationToken cancellationToken)
    {
        var request = await EmailableRequestAsync(command.RequestId, cancellationToken);
        var recipients = await RecipientsForAsync(request, command.RecipientOverride, cancellationToken);

        var model = await RequestDocumentBuilder.BuildAsync(context, command.RequestId, cancellationToken, recipients);
        if (model is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var draft = await StagedDraftAsync(request, model, recipients, cancellationToken);
        var filing = new OutboundEmailFiling(
            "Client", StagingRefused, request.ProjectId,
            RecordType.Request, request.RequestId, model.DisplayNumber);

        var dispatch = await dispatcher.DispatchAsync(draft, filing, command.SaveAsDraftOnly, cancellationToken);
        await RequestLifecycle.OpenIfNeedsActionAsync(context, request, cancellationToken);
        return OutcomeFor(request, model, recipients, dispatch);
    }

    /// <summary>EMAIL POLICY: only RFI / NOD / EOT documents are ever drafted for sending.</summary>
    private async Task<RequestEntity> EmailableRequestAsync(string requestId, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == requestId, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{requestId}' not found.");

        var kind = (RequestType)request.Kind;
        if (kind.IsEmailable()) return request;

        throw new InvalidOperationException(
            $"A {kind.DisplayName()} request is never emailed — only RFI, NOD and EOT documents " +
            "are drafted for sending. Promote the request first if it should go out as an RFI.");
    }

    private RequestEmailOutcome OutcomeFor(
        RequestEntity request, RequestDocumentModel model, RequestRecipientSet recipients, OutboundEmailDispatch dispatch) =>
        new(request.RequestId,
            model.EmailSubject,
            recipients.To.Select(recipient => recipient.Email).ToList(),
            dispatch.WebLink,
            Cc: CopiedRecipients(recipients),
            Bcc: recipients.Bcc.Select(recipient => recipient.Email).ToList(),
            DraftMessageId: dispatch.MessageId,
            Sent: dispatch.Sent,
            FailureNote: dispatch.FailureNote);
}
