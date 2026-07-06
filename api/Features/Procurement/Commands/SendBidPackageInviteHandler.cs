using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Sends the reviewed tender-invite email from the shared mailbox. The mailbox itself is the To
// (subcontractors must not see each other), every recipient with a directory email goes in BCC, and
// the sent copy carries the package's tag ("JPMS/BPI-0001") so it — and, more importantly, the
// replies triaged onto the same tag — group under the package. Moves a Draft package to Inviting.
public sealed class SendBidPackageInviteHandler : ICommandHandler<SendBidPackageInvite, BidPackage>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;
    private readonly MailboxIntakeOptions options;

    public SendBidPackageInviteHandler(JpmsContext context, IMailboxGraphClient mailbox, MailboxIntakeOptions options)
    {
        this.context = context; this.mailbox = mailbox; this.options = options;
    }

    public async Task<BidPackage> HandleAsync(SendBidPackageInvite command, CancellationToken cancellationToken)
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
                "No invited subcontractors with an email address in the directory — add recipients before sending.");

        var recipients = bcc
            .GroupBy(r => r.ContactEmail, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MailboxDraftRecipient(g.Key, g.First().CompanyName))
            .ToList();

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(options.Mailbox) },
            Subject: command.Subject,
            HtmlBody: command.HtmlBody,
            Attachments: Array.Empty<MailboxDraftAttachment>(),
            Bcc: recipients,
            Categories: new[] { TriageCategories.Marker, TriageCategories.ForRecord(package.Reference) });

        var sent = await mailbox.SendMailAsync(message, cancellationToken);
        if (!sent)
            throw new InvalidOperationException(
                "The mailbox couldn't send the invite. Check the app registration has the Mail.Send " +
                "application permission for the shared mailbox, then try again.");

        if (package.Status == (int)BidPackageStatus.Draft)
            package.Status = (int)BidPackageStatus.Inviting;
        await context.SaveChangesAsync(cancellationToken);

        return package.ToModel();
    }
}
