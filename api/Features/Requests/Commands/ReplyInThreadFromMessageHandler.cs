using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Triage "Reply in thread": the triager writes the reply in the portal, and this one command turns
/// it into both halves of the triage. First it creates a General request from the email (delegating
/// to the same handler as "Create new → Request", so the email — and its whole conversation, so the
/// entire thread leaves the queue — is tagged to the new request first, the anchor verified, and the
/// request is auto-numbered REQ-####) whose description carries the written reply
/// ("Replied to email in thread with: …"). Then it stages that reply as an Outlook draft on the
/// email in the projects mailbox: Graph's createReplyAll keeps the draft in the original
/// conversation — "RE:" subject, thread headers, quoted history, original recipients — the written
/// reply sits above the quoted history as the draft's body, and the draft carries the request's
/// workflow category so the sent copy and its replies group under the request in triage. Unlike
/// <see cref="SendRequestReplyHandler"/> no document is attached; the pre-filled draft is
/// reviewed and sent from the mailbox itself — the dispatcher is asked for a draft and stops there,
/// code never sends. If the draft can't be staged, the just-created request is rolled back (tag
/// removed, request deleted) so the email stays in the queue rather than being triaged without a
/// reply.
/// </summary>
public sealed partial class ReplyInThreadFromMessageHandler : ICommandHandler<ReplyInThreadFromMessage, ReplyInThreadOutcome>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly OutboundEmailDispatcher dispatcher;
    private readonly ICommandHandler<CreateRequestFromMessage, Request> createRequest;

    public ReplyInThreadFromMessageHandler(
        JpmsContext context,
        IMailboxGraphClient graph,
        OutboundEmailDispatcher dispatcher,
        ICommandHandler<CreateRequestFromMessage, Request> createRequest)
    {
        this.context = context;
        this.graph = graph;
        this.dispatcher = dispatcher;
        this.createRequest = createRequest;
    }

    public async Task<ReplyInThreadOutcome> HandleAsync(ReplyInThreadFromMessage command, CancellationToken cancellationToken)
    {
        // The written reply is the whole point of this action — an empty draft helps no one.
        var reply = command.ReplyBody?.Trim() ?? "";
        if (reply.Length == 0)
            throw new InvalidOperationException("Write the reply before creating the draft.");

        // Read the email first so the request's title is built from the mailbox's own copy (the
        // client only sends ids), and so a vanished email fails before anything is created.
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");

        var request = await RaisedRequestAsync(command, snapshot, reply, cancellationToken);
        var tag = await WorkflowTagAsync(command, request, cancellationToken);
        var dispatch = await StagedReplyAsync(command, request, tag, ToHtml(reply), cancellationToken);
        request = await MovedToOpenAsync(request, cancellationToken);

        return new ReplyInThreadOutcome(
            request,
            new RequestEmailOutcome(
                request.RequestId, dispatch.Subject, dispatch.To ?? Array.Empty<string>(), dispatch.WebLink,
                Cc: dispatch.Cc, DraftMessageId: dispatch.MessageId));
    }

    // Portal textarea (plain text) -> draft HTML: encode each line, join with <br>, and leave a
    // blank line before the quoted history the cover note is prepended to.
    private static string ToHtml(string reply) =>
        "<div>"
        + string.Join("<br>", reply.Replace("\r\n", "\n").Split('\n').Select(System.Net.WebUtility.HtmlEncode))
        + "</div><br>";
}
