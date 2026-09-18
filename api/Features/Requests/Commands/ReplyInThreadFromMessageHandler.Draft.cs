using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed partial class ReplyInThreadFromMessageHandler
{
    private const string StagingRefused =
        "The reply draft couldn't be created in the projects mailbox, so nothing was triaged — "
        + "the email is still in the queue. The original email may no longer be there, or the "
        + "mailbox connection failed — check and try again.";

    /// <summary>The written reply above the quoted history Graph supplies, staged and left in
    /// Drafts for the triager to review and send from Outlook. No attachment; tagged so the sent
    /// copy groups under the new request in triage. The draft leaves an audit row like every other
    /// portal email, which is what a reply staged this way never used to do.</summary>
    private async Task<OutboundEmailDispatch> StagedReplyAsync(
        ReplyInThreadFromMessage command,
        Request request,
        string tag,
        string replyHtml,
        CancellationToken cancellationToken)
    {
        var reply = new MailboxReplyDraftMessage(
            command.MessageId,
            HtmlCoverNote: replyHtml,
            Attachments: Array.Empty<MailboxDraftAttachment>(),
            Categories: new[] { TriageCategories.Marker, tag, TriageCategories.Client });
        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Client), StagingRefused,
            command.ProjectId, RecordType.Request, request.RequestId, request.Reference.Trim());
        try
        {
            return await dispatcher.DispatchReplyAsync(reply, filing, saveAsDraftOnly: true, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await RolledBackAsync(request, tag, cancellationToken);
            throw;
        }
    }

    /// <summary>Roll the background request back (best-effort) so the email returns to the queue —
    /// half-triaged (request created, no reply staged) is worse than not triaged at all.</summary>
    private async Task RolledBackAsync(Request request, string tag, CancellationToken cancellationToken)
    {
        try { await graph.ClearRequestTagsAsync(tag, cancellationToken); } catch { /* best-effort */ }
        var entity = await context.Requests.FirstOrDefaultAsync(row => row.RequestId == request.RequestId, cancellationToken);
        if (entity is null) return;
        context.Requests.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
