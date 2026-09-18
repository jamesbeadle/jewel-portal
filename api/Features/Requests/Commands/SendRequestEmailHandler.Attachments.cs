using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed partial class SendRequestEmailHandler
{
    /// <summary>
    /// Reads the request's uploaded files back out of blob storage so they can ride on the draft.
    /// A site photograph is usually the clearest thing in an RFI — the whole reason a site manager
    /// took it was so the architect could see what they were being asked about — so it belongs on
    /// the email, not only in the portal. Drawing LINKS are not sent: the architect issued those
    /// drawings, and the PDF already cites them by code and revision.
    ///
    /// Best-effort by design: a file that has gone missing, or storage being unreachable, must not
    /// stop the RFI going out — the document itself is the thing that matters, and the person
    /// reviewing the draft in Outlook can see what is attached before they send it.
    /// </summary>
    private async Task<List<MailboxDraftAttachment>> LoadRequestFileAttachmentsAsync(
        string requestId, CancellationToken cancellationToken)
    {
        var attachments = new List<MailboxDraftAttachment>();

        var rows = await context.RequestAttachments
            .AsNoTracking()
            .Where(row => row.RequestId == requestId && row.Kind == (int)RequestAttachmentKind.File)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            var attachment = await TryReadAsync(row, cancellationToken);
            if (attachment is not null) attachments.Add(attachment);
        }

        return attachments;
    }

    private async Task<MailboxDraftAttachment?> TryReadAsync(
        RequestAttachmentEntity row, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(row.BlobRef)) return null;
        try
        {
            var blob = await attachmentStore.OpenAsync(row.BlobRef, cancellationToken);
            if (blob is null) return null;
            await using var content = blob.Content;
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            return new MailboxDraftAttachment(
                string.IsNullOrWhiteSpace(row.FileName) ? "attachment" : row.FileName,
                row.ContentType ?? blob.ContentType,
                buffer.ToArray());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null; // skip this one and carry on — see the note above
        }
    }

    /// <summary>Mints a download link per file, or null if ANY link fails — the caller then
    /// attaches everything instead, because an email promising links it doesn't carry is worse
    /// than a draft Outlook refuses to send (the reviewer can still trim it there).</summary>
    private async Task<List<EmailFileShareLink>?> TryShareAsync(
        IReadOnlyList<MailboxDraftAttachment> toLink, string scope, CancellationToken cancellationToken)
    {
        var links = new List<EmailFileShareLink>();
        foreach (var file in toLink)
        {
            try
            {
                var link = await shareStore.ShareAsync(scope, file.FileName, file.ContentType, file.Content, cancellationToken);
                if (link is null) return null;
                links.Add(link);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return null;
            }
        }
        return links;
    }
}
