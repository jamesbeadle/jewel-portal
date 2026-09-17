using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Sends the tender-invite email from the shared projects mailbox — the composer's door, the
/// counterpart of SendBidPackageInviteToTenderList, sharing its attachment plan through
/// BidPackageInviteMailAssembler. The composer's envelope is authoritative: whatever To/Cc/Bcc it
/// shows is exactly what goes on the wire (an empty To is addressed to the mailbox itself, the
/// house convention for BCC fan-out). Staging, the send, the degrade back to a draft and the audit
/// row are the dispatcher's, so a failed send leaves the reviewed draft in the mailbox's Drafts
/// folder and says so, never losing the email. A successful send clears the package's persisted
/// composer draft: it has served its purpose.
/// </summary>
public sealed partial class SendBidPackageInviteHandler : ICommandHandler<SendBidPackageInvite, BidPackageInviteSendOutcome>
{
    private const string StagingRefused =
        "The invite couldn't be staged in the shared mailbox, so nothing was sent. "
        + "Check the mailbox connection, then try again here.";

    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;
    private readonly MailboxIntakeOptions options;
    private readonly BidPackageInviteMailAssembler assembler;

    public SendBidPackageInviteHandler(
        JpmsContext context, OutboundEmailDispatcher dispatcher, MailboxIntakeOptions options,
        BidPackageInviteMailAssembler assembler)
    {
        this.context = context; this.dispatcher = dispatcher; this.options = options;
        this.assembler = assembler;
    }

    public async Task<BidPackageInviteSendOutcome> HandleAsync(SendBidPackageInvite command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        var to = ParseRecipients(command.To);
        var cc = ParseRecipients(command.Cc);
        var bcc = ParseRecipients(command.Bcc);

        if (to.Count == 0 && cc.Count == 0 && bcc.Count == 0)
            throw new InvalidOperationException("The invite has no recipients — add at least one address before sending.");

        // The house convention for BCC fan-out: the mailbox itself takes the To when the composer
        // leaves it empty, so no subcontractor is ever the visible addressee of a mass invite.
        if (to.Count == 0)
            to.Add(new MailboxDraftRecipient(options.Mailbox));

        var plan = await assembler.PlanAsync(package, command.HtmlBody, cancellationToken);

        var message = new MailboxDraftMessage(
            To: to,
            Subject: command.Subject,
            HtmlBody: plan.HtmlBody,
            Attachments: plan.Attach,
            Bcc: bcc,
            // Record tag + Subcontractor pathway: the invite thread is born filed on the
            // subcontractor side, and replies inherit both through the thread sweep.
            Categories: new[] { TriageCategories.Marker, TriageCategories.ForRecord(package.Reference), TriageCategories.Subcontractor },
            Cc: cc);

        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Subcontractor),
            StagingRefused,
            package.ProjectId,
            RecordType.BidPackageInvite,
            package.BidPackageId,
            package.Reference);

        var dispatch = await dispatcher.DispatchAsync(message, filing, saveAsDraftOnly: false, cancellationToken);
        var attachedFiles = plan.Attach.Select(file => file.FileName).ToList();
        var recipientCount = to.Count + cc.Count + bcc.Count;

        if (dispatch.Sent) await ClearComposerDraftAsync(package, cancellationToken);

        return new BidPackageInviteSendOutcome(
            package.ToModel(), dispatch.Sent, dispatch.WebLink, recipientCount, plan.LinkedFiles,
            FailureNote: dispatch.FailureNote, AttachedFiles: attachedFiles);
    }
}
