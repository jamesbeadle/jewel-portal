using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Emails the request's official document as a REPLY to an email already linked to it. Graph's
/// createReplyAll keeps it in the original conversation — "RE:" subject, thread headers, quoted
/// history, original recipients — and the branded cover note sits above the quoted history, so the
/// formal document arrives inside the email chain it relates to. Recipients are NOT resolved here:
/// a reply inherits the conversation's participants, which is the whole point. The sent copy
/// carries the request's workflow tag, so it and its replies group under the request in triage.
///
/// Sending means it is going out, so a Needs-action request moves to Open — with the correspondent,
/// awaiting their response. A request already past Needs action keeps its status: re-sending never
/// rewinds a lifecycle.
/// </summary>
public sealed class SendRequestReplyHandler : ICommandHandler<SendRequestReply, RequestEmailOutcome>
{
    private const string StagingRefused =
        "The reply couldn't be staged in the projects mailbox, so nothing was sent. The original "
        + "email may no longer be there, or the mailbox connection failed — check and try again.";

    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendRequestReplyHandler(JpmsContext context, OutboundEmailDispatcher dispatcher)
    {
        this.context = context;
        this.dispatcher = dispatcher;
    }

    public async Task<RequestEmailOutcome> HandleAsync(SendRequestReply command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        // EMAIL POLICY: the official document only exists at the official stage (RFI / NOD / EOT),
        // so only those kinds can be replied with it attached.
        var kind = (RequestType)request.Kind;
        if (!kind.IsEmailable())
            throw new InvalidOperationException(
                $"A {kind.DisplayName()} request has no official document to send — promote it to an " +
                "RFI first, then reply into the thread with the official PDF.");

        var model = await RequestDocumentBuilder.BuildAsync(context, command.RequestId, cancellationToken);
        if (model is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var reply = new MailboxReplyDraftMessage(
            command.MailboxMessageId,
            RequestEmailComposer.BuildCoverNote(model),
            new[] { new MailboxDraftAttachment(model.FileName, "application/pdf", RequestDocumentRenderer.Render(model)) },
            new[]
            {
                TriageCategories.Marker,
                TriageCategories.ForRecord(await RequestTags.StemAsync(context, request, cancellationToken)),
                TriageCategories.Client
            });

        var filing = new OutboundEmailFiling(
            "Client", StagingRefused, request.ProjectId,
            RecordType.Request, request.RequestId, model.DisplayNumber);

        var dispatch = await dispatcher.DispatchReplyAsync(reply, filing, command.SaveAsDraftOnly, cancellationToken);
        await RequestLifecycle.OpenIfNeedsActionAsync(context, request, cancellationToken);

        return new RequestEmailOutcome(
            request.RequestId,
            dispatch.Subject,
            dispatch.To ?? Array.Empty<string>(),
            dispatch.WebLink,
            Cc: dispatch.Cc,
            DraftMessageId: dispatch.MessageId,
            Sent: dispatch.Sent,
            FailureNote: dispatch.FailureNote);
    }
}
