using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Drafts the reviewed tender-invite email in the shared mailbox — nothing is sent. The mailbox
// itself is the To (subcontractors must not see each other), every recipient with a directory email
// goes in BCC, and the draft carries the package's tag ("JPMS/BPI-0001") so the copy that is
// eventually sent from Outlook — and the replies triaged onto the same tag — group under the
// package. What travels with it (the generated pricing schedule, the company terms, tender
// documents, linked drawings, the 25 MB overflow-to-links rule) is planned by
// BidPackageInviteMailAssembler — one plan shared with the in-app send path, so the two can never
// disagree. Package status is untouched: inviting recipients already moved a Draft package to
// Inviting, and the actual send happens in Outlook.
public sealed class PrepareBidPackageInviteDraftHandler : ICommandHandler<PrepareBidPackageInviteDraft, BidPackageInviteDraft>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;
    private readonly MailboxIntakeOptions options;
    private readonly BidPackageInviteMailAssembler assembler;

    public PrepareBidPackageInviteDraftHandler(
        JpmsContext context, IMailboxGraphClient mailbox, MailboxIntakeOptions options,
        BidPackageInviteMailAssembler assembler)
    {
        this.context = context; this.mailbox = mailbox; this.options = options;
        this.assembler = assembler;
    }

    public async Task<BidPackageInviteDraft> HandleAsync(PrepareBidPackageInviteDraft command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        var recipients = await assembler.DefaultBccAsync(command.BidPackageId, cancellationToken);
        if (recipients.Count == 0)
            throw new InvalidOperationException(
                "No invited subcontractors with an email address in the directory — add recipients before drafting.");

        var plan = await assembler.PlanAsync(package, command.HtmlBody, cancellationToken);

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(options.Mailbox) },
            Subject: command.Subject,
            HtmlBody: plan.HtmlBody,
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
            LinkedFiles: plan.LinkedFiles);
    }
}
