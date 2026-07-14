using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Triage "Reply in thread": the triager writes the reply in the portal, and this one command turns
/// it into both halves of the triage. First it creates a General request from the email (delegating
/// to the same handler as "Create new → Request", so the email is tagged to the new request first,
/// verified, and the request is auto-numbered REQ-####) whose description carries the written reply
/// ("Replied to email in thread with: …"). Then it stages that reply as an Outlook draft on the
/// email in the projects mailbox: Graph's createReplyAll keeps the draft in the original
/// conversation — "RE:" subject, thread headers, quoted history, original recipients — the written
/// reply sits above the quoted history as the draft's body, and the draft carries the request's
/// workflow category so the sent copy and its replies group under the request in triage. Unlike
/// <see cref="PrepareRequestReplyDraftHandler"/> no document is attached; the pre-filled draft is
/// reviewed and sent from the mailbox itself — code never sends. If the draft can't be staged, the
/// just-created request is rolled back (tag removed, request deleted) so the email stays in the
/// queue rather than being triaged without a reply.
/// </summary>
public sealed class ReplyInThreadFromMessageHandler : ICommandHandler<ReplyInThreadFromMessage, ReplyInThreadOutcome>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly ICommandHandler<CreateRequestFromMessage, Request> createRequest;

    public ReplyInThreadFromMessageHandler(
        JpmsContext context,
        IMailboxGraphClient graph,
        ICommandHandler<CreateRequestFromMessage, Request> createRequest)
    {
        this.context = context;
        this.graph = graph;
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

        var subject = string.IsNullOrWhiteSpace(snapshot.Subject) ? "(no subject)" : snapshot.Subject.Trim();

        // The request records HOW it was triaged — the reply written in the portal IS the request's
        // content, so the request page reads as "this is what we answered".
        var description = $"Replied to email in thread with:\n\n{reply}";

        // Create the General request exactly as "Create new → Request" would: auto-numbered
        // REQ-#### (blank reference), email tagged to it first and verified before the request
        // persists. This is the background half of the action — the paper trail for the reply.
        var request = await createRequest.HandleAsync(
            new CreateRequestFromMessage(
                command.MessageId,
                command.ProjectId,
                RequestType.General,
                Reference: "",
                Title: subject,
                Description: description,
                InternetMessageId: command.InternetMessageId ?? snapshot.InternetMessageId,
                RaisedByEmail: command.RaisedByEmail),
            cancellationToken);

        // The same project-qualified workflow tag the create path stamped on the email, re-derived
        // here for the draft's categories (a General request's minted reference is never blank).
        var tag = TriageCategories.ForRecord(
            RequestTags.Stem(
                await RequestTags.ProjectRefAsync(context, command.ProjectId, cancellationToken),
                command.ProjectId,
                request.Reference.Trim()));

        // Stage the reply draft with the written reply as its body, sitting above the quoted
        // history Graph supplies — the triager reviews and presses Send in Outlook, nothing more.
        // Plain text from the portal textarea is HTML-encoded line by line so nothing in it can
        // inject markup into the draft. No attachment; tagged so the sent copy groups under the
        // new request in triage.
        var created = await graph.CreateReplyDraftAsync(
            new MailboxReplyDraftMessage(
                command.MessageId,
                HtmlCoverNote: ToHtml(reply),
                Attachments: Array.Empty<MailboxDraftAttachment>(),
                Categories: new[] { TriageCategories.Marker, tag }),
            cancellationToken);

        if (created is null)
        {
            // Roll the background request back (best-effort) so the email returns to the queue —
            // half-triaged (request created, no reply staged) is worse than not triaged at all.
            try { await graph.ClearRequestTagsAsync(tag, cancellationToken); } catch { /* best-effort */ }
            var entity = await context.Requests.FirstOrDefaultAsync(r => r.RequestId == request.RequestId, cancellationToken);
            if (entity is not null)
            {
                context.Requests.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }
            throw new InvalidOperationException(
                "The reply draft couldn't be created in the projects mailbox, so nothing was triaged — " +
                "the email is still in the queue. The original email may no longer be there, or the " +
                "mailbox connection failed — check and try again.");
        }

        return new ReplyInThreadOutcome(
            request,
            new RequestEmailDraft(request.RequestId, created.Subject, created.To, created.WebLink, Cc: created.Cc));
    }

    // Portal textarea (plain text) -> draft HTML: encode each line, join with <br>, and leave a
    // blank line before the quoted history the cover note is prepended to.
    private static string ToHtml(string reply) =>
        "<div>"
        + string.Join("<br>", reply.Replace("\r\n", "\n").Split('\n').Select(System.Net.WebUtility.HtmlEncode))
        + "</div><br>";
}
