using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class SendMailboxEmailHandler
{
    /// <summary>Bytes in hand before anything is created: the attachments resolved, the body
    /// sanitised (pasted images become inline attachments), and anything over the Exchange
    /// ceiling turned into download links.</summary>
    private async Task ResolveBodyAndAttachmentsAsync(
        Compose compose, IReadOnlyDictionary<string, UploadedFile>? uploads, CancellationToken cancellationToken)
    {
        var command = compose.Command;
        var attachments = await ResolveAttachmentsAsync(command, uploads, cancellationToken);

        var composed = command.BodyIsHtml
            ? pipeline.FromHtml(command.Body)
            : new ComposeHtmlPipeline.ComposedBody(ComposeHtmlPipeline.FromPlainText(command.Body), Array.Empty<MailboxDraftAttachment>());
        compose.BodyHtml = composed.Html;

        // Inline (cid) images are part of the body and never linked; their bytes are reserved out
        // of the budget instead. The largest ordinary attachments move to links until what remains
        // fits the Exchange ceiling — so the email always goes, with as much attached as fits.
        var inlineBytes = composed.InlineImages.Sum(a => a.Content.LongLength);
        var plan = EmailAttachmentPlanner.Split(attachments, reservedBytes: inlineBytes);
        if (plan.ToLink.Count > 0)
        {
            compose.BodyHtml += await LinkOversizedAsync(command, plan.ToLink, cancellationToken);
            attachments = plan.Attach.ToList();
        }

        var attached = attachments.Concat(composed.InlineImages).ToList();
        compose.Attachments = attached;
        if (attached.Sum(a => a.Content.LongLength) > MaxTotalAttachmentBytes)
            throw new InvalidOperationException(
                "The images pasted into the email total more than 25 MB on their own — remove some and try again.");
    }

    /// <summary>The over-budget files as 7-day download links, returned as the HTML block that
    /// goes at the foot of the body.</summary>
    private async Task<string> LinkOversizedAsync(
        SendMailboxEmail command, IReadOnlyList<MailboxDraftAttachment> toLink, CancellationToken cancellationToken)
    {
        if (!shareStore.IsConfigured)
            throw new InvalidOperationException(
                "The attachments total more than 25 MB, and the file-share store isn't configured to turn the " +
                "largest into download links — remove some files and try again.");

        var shareScope = NullIfEmpty(command.ProjectId) ?? "compose";
        var links = new List<EmailFileShareLink>();
        foreach (var file in toLink)
        {
            var link = await shareStore.ShareAsync(shareScope, file.FileName, file.ContentType, file.Content, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"A download link couldn't be created for {file.FileName}. Nothing was sent — try again.");
            links.Add(link);
        }
        return EmailAttachmentPlanner.LinksHtmlBlock(links);
    }
}
