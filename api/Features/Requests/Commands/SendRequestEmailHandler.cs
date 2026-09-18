using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Emails the request's official document from the projects mailbox as a new thread.
/// SaveAsDraftOnly stops after staging, leaving the reviewed draft in Drafts for Outlook, which is
/// what this handler did for everybody until 2026-09-17.
///
/// It composes nothing itself. The message is <see cref="RequestEmailComposer"/>'s, so the preview
/// a person reads is the email this sends; staging, the send, the degrade back to a draft and the
/// audit row are the dispatcher's; the status move is RequestLifecycle's, shared with the reply.
/// </summary>
public sealed class SendRequestEmailHandler : ICommandHandler<SendRequestEmail, RequestEmailOutcome>
{
    private const string StagingRefused =
        "The email couldn't be staged in the projects mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly JpmsContext context;
    private readonly RequestEmailComposer composer;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendRequestEmailHandler(
        JpmsContext context, RequestEmailComposer composer, OutboundEmailDispatcher dispatcher)
    {
        this.context = context;
        this.composer = composer;
        this.dispatcher = dispatcher;
    }

    public async Task<RequestEmailOutcome> HandleAsync(SendRequestEmail command, CancellationToken cancellationToken)
    {
        var composed = await composer.ComposeAsync(
            command.RequestId, command.RecipientOverride, cancellationToken);

        var filing = new OutboundEmailFiling(
            "Client", StagingRefused, composed.ProjectId,
            RecordType.Request, command.RequestId, composed.Reference);

        var dispatch = await dispatcher.DispatchAsync(
            composed.Message, filing, command.SaveAsDraftOnly, cancellationToken);

        var request = await context.Requests
            .FirstOrDefaultAsync(row => row.RequestId == command.RequestId, cancellationToken);
        if (request is not null) await RequestLifecycle.OpenIfNeedsActionAsync(context, request, cancellationToken);

        return OutcomeFor(command.RequestId, composed, dispatch);
    }

    private static RequestEmailOutcome OutcomeFor(
        string requestId, ComposedRecordEmail composed, OutboundEmailDispatch dispatch) =>
        new(requestId,
            composed.Message.Subject,
            composed.Message.To.Select(recipient => recipient.Email).ToList(),
            dispatch.WebLink,
            Cc: composed.CopiedTo,
            Bcc: (composed.Message.Bcc ?? Array.Empty<MailboxIntake.Graph.MailboxDraftRecipient>())
                .Select(recipient => recipient.Email).ToList(),
            DraftMessageId: dispatch.MessageId,
            Sent: dispatch.Sent,
            FailureNote: dispatch.FailureNote);
}
