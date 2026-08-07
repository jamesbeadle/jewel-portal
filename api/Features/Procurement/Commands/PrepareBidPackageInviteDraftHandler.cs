using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Drafts the reviewed tender-invite email in the shared mailbox — nothing is sent. The mailbox
// itself is the To (subcontractors must not see each other), every recipient with a directory email
// goes in BCC, and the draft carries the package's tag ("JPMS/BPI-0001") so the copy that is
// eventually sent from Outlook — and the replies triaged onto the same tag — group under the
// package. The package's linked drawings are attached (latest approved revision, or the newest
// upload when none is approved) — except when they would push the email past the ~25 MB Exchange
// ceiling, in which case the largest files are copied to the email-shares container and travel as
// 7-day download links in the body instead (an oversized draft would otherwise stage fine and only
// fail when a person presses Send in Outlook). Package status is untouched: inviting recipients
// already moved a Draft package to Inviting, and the actual send happens in Outlook.
public sealed class PrepareBidPackageInviteDraftHandler : ICommandHandler<PrepareBidPackageInviteDraft, BidPackageInviteDraft>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;
    private readonly MailboxIntakeOptions options;
    private readonly IDrawingBlobStore blobStore;
    private readonly IEmailFileShareStore shareStore;

    public PrepareBidPackageInviteDraftHandler(
        JpmsContext context, IMailboxGraphClient mailbox, MailboxIntakeOptions options,
        IDrawingBlobStore blobStore, IEmailFileShareStore shareStore)
    {
        this.context = context; this.mailbox = mailbox; this.options = options;
        this.blobStore = blobStore; this.shareStore = shareStore;
    }

    public async Task<BidPackageInviteDraft> HandleAsync(PrepareBidPackageInviteDraft command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        // BCC list: every invited subcontractor with an email in the directory.
        var bcc = await (
            from recipient in context.BidPackageRecipients
            where recipient.BidPackageId == command.BidPackageId
            join sub in context.Subcontractors on recipient.SubcontractorId equals sub.SubcontractorId
            where sub.ContactEmail != null && sub.ContactEmail != ""
            select new { sub.ContactEmail, sub.CompanyName })
            .ToListAsync(cancellationToken);

        if (bcc.Count == 0)
            throw new InvalidOperationException(
                "No invited subcontractors with an email address in the directory — add recipients before drafting.");

        var recipients = bcc
            .GroupBy(r => r.ContactEmail, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MailboxDraftRecipient(g.Key, g.First().CompanyName))
            .ToList();

        // Attach what fits; anything that would push the email past the ceiling goes out as a
        // 7-day download link in the body instead.
        var drawings = await LoadDrawingAttachmentsAsync(command.BidPackageId, cancellationToken);
        var plan = EmailAttachmentPlanner.Split(drawings);
        var htmlBody = command.HtmlBody;
        var linkedFiles = new List<string>();

        if (plan.ToLink.Count > 0)
        {
            if (!shareStore.IsConfigured)
                throw new InvalidOperationException(
                    $"The linked drawings total {EmailAttachmentPlanner.FormatSize(drawings.Sum(a => a.Content.LongLength))} — " +
                    "more than fits one email — and the file-share store isn't configured, so download links can't be " +
                    "created. Unlink some drawings, or configure DrawingsStorage:ConnectionString.");

            var links = new List<EmailFileShareLink>();
            foreach (var file in plan.ToLink)
            {
                var link = await shareStore.ShareAsync(
                        package.Reference, file.FileName, file.ContentType, file.Content, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"A download link couldn't be created for {file.FileName}. Nothing was drafted — try again.");
                links.Add(link);
                linkedFiles.Add(file.FileName);
            }
            htmlBody += EmailAttachmentPlanner.LinksHtmlBlock(links, "Tender documents — download links");
        }

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(options.Mailbox) },
            Subject: command.Subject,
            HtmlBody: htmlBody,
            Attachments: plan.Attach,
            Bcc: recipients,
            // Record tag + Subcontractor pathway: the invite thread is born filed on the
            // subcontractor side, and replies inherit both through the thread sweep.
            Categories: new[] { TriageCategories.Marker, TriageCategories.ForRecord(package.Reference), TriageCategories.Subcontractor });

        var draft = await mailbox.CreateDraftAsync(message, cancellationToken);
        if (draft is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the shared mailbox. Check the mailbox connection and try again.");

        return new BidPackageInviteDraft(
            package.ToModel(),
            command.Subject,
            recipients.Select(r => r.Email).ToList(),
            draft.WebLink,
            LinkedFiles: linkedFiles);
    }

    // One attachment per linked drawing: its latest approved revision, or — when nothing is approved
    // yet — the newest uploaded revision. Drawings whose file can't be opened are skipped rather than
    // blocking the draft; the linked list on the package remains the source of truth.
    private async Task<IReadOnlyList<MailboxDraftAttachment>> LoadDrawingAttachmentsAsync(string bidPackageId, CancellationToken cancellationToken)
    {
        var revisions = await (
            from link in context.BidPackageDrawings
            where link.BidPackageId == bidPackageId
            join revision in context.DrawingRevisions on link.DrawingId equals revision.DrawingId
            where revision.BlobRef != null
            select revision)
            .ToListAsync(cancellationToken);

        var attachments = new List<MailboxDraftAttachment>();
        foreach (var chosen in revisions
            .GroupBy(r => r.DrawingId)
            .Select(g => g
                .OrderByDescending(r => r.ApprovalStatus == (int)DrawingApprovalStatus.Approved)
                .ThenByDescending(r => r.ReceivedAt)
                .First()))
        {
            var blob = await blobStore.OpenAsync(chosen.BlobRef!, cancellationToken);
            if (blob is null) continue;

            await using var stream = blob.Content;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            attachments.Add(new MailboxDraftAttachment(
                chosen.FileName,
                string.IsNullOrWhiteSpace(chosen.ContentType) ? "application/octet-stream" : chosen.ContentType!,
                buffer.ToArray()));
        }
        return attachments;
    }
}
