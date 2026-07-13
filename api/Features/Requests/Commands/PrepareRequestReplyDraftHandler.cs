using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Creates an Outlook draft REPLY to an email already linked to the request, carrying the freshly
/// rendered official document PDF. Graph's createReplyAll keeps the reply in the original
/// conversation — "RE:" subject, thread headers, quoted history, original recipients — and the
/// branded cover note is placed above the quoted history, so the formal document arrives inside the
/// email chain it relates to. The draft is tagged with the request's workflow category so the sent
/// copy and its replies group under the request in triage. Nothing is sent and no status changes —
/// a person reviews, adjusts recipients if needed, and sends from the mailbox itself.
/// </summary>
public sealed class PrepareRequestReplyDraftHandler : ICommandHandler<PrepareRequestReplyDraft, RequestEmailDraft>
{
    private readonly JpmsContext context;
    private readonly RequestEmailReader emails;
    private readonly IMailboxGraphClient graph;

    public PrepareRequestReplyDraftHandler(JpmsContext context, RequestEmailReader emails, IMailboxGraphClient graph)
    {
        this.context = context;
        this.emails = emails;
        this.graph = graph;
    }

    public async Task<RequestEmailDraft> HandleAsync(PrepareRequestReplyDraft command, CancellationToken cancellationToken)
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

        // Recipients are NOT resolved here — a reply inherits the original conversation's
        // participants from Graph, which is the point: the document lands in the existing thread.
        var tagged = await emails.ForRequestAsync(command.RequestId, cancellationToken);
        var model = await RequestDocumentBuilder.BuildAsync(context, command.RequestId, tagged, cancellationToken);
        if (model is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var pdf = RequestDocumentRenderer.Render(model);
        var tag = TriageCategories.ForRecord(await RequestTags.StemAsync(context, request, cancellationToken));

        var created = await graph.CreateReplyDraftAsync(
            new MailboxReplyDraftMessage(
                command.MailboxMessageId,
                PrepareRequestEmailDraftHandler.BuildCoverNote(model),
                new[] { new MailboxDraftAttachment(model.FileName, "application/pdf", pdf) },
                new[] { TriageCategories.Marker, tag }),
            cancellationToken);
        if (created is null)
            throw new InvalidOperationException(
                "The reply draft couldn't be created in the projects mailbox. The original email may " +
                "no longer be there, or the mailbox connection failed — check and try again.");

        return new RequestEmailDraft(
            request.RequestId,
            created.Subject,
            created.To,
            created.WebLink,
            Cc: created.Cc);
    }
}
