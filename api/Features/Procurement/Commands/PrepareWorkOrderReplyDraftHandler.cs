using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Documents;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Creates an Outlook draft REPLY to an email already linked to the work order, carrying the freshly
// rendered purchase-order PDF. Graph's createReplyAll keeps the reply in the original conversation —
// "RE:" subject, thread headers, quoted history, original recipients — and the caller's cover note
// is placed above the quoted history, so the formal purchase order arrives inside the email chain
// the works were agreed in (the request flow's PrepareRequestReplyDraftHandler, retold for
// procurement). The draft carries the order's workflow tag (and the source package's, when there is
// one) so the sent copy and the supplier's replies group under the order. Nothing is sent — a
// person reviews, adjusts recipients if needed, and sends from the mailbox itself — and unlike the
// request flow there is NO status side effect: a work order's lifecycle is driven by approval and
// acceptance, never by drafting its covering email.
public sealed class PrepareWorkOrderReplyDraftHandler : ICommandHandler<PrepareWorkOrderReplyDraft, WorkOrderReplyDraft>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;
    private readonly AuditTrail audit;

    public PrepareWorkOrderReplyDraftHandler(JpmsContext context, IMailboxGraphClient mailbox, AuditTrail audit)
    {
        this.context = context; this.mailbox = mailbox; this.audit = audit;
    }

    public async Task<WorkOrderReplyDraft> HandleAsync(PrepareWorkOrderReplyDraft command, CancellationToken cancellationToken)
    {
        var order = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");

        // Same promises as the fresh-draft and send handlers, kept against direct calls — plus
        // Cancelled, which the PO page keeps un-emailable: a voided order's PO would announce a
        // commitment that no longer stands.
        if (order.Status == (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException(
                "This work order is still a draft — approve it before emailing the supplier.");
        if (order.Status == (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException(
                "This work order was rejected — it is never sent to the supplier.");
        if (order.Status == (int)WorkOrderStatus.Cancelled)
            throw new InvalidOperationException(
                "This work order was cancelled — its purchase order is a voided record and is never emailed.");

        // Recipients are NOT resolved here — a reply inherits the original conversation's
        // participants from Graph, which is the point: the purchase order lands in the existing
        // thread. The builder resolves everything the PDF prints (supplier, project, paid
        // position), mirroring the PO page.
        var model = await WorkOrderPoDocumentBuilder.BuildAsync(context, command.WorkOrderId, cancellationToken);
        if (model is null) throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");

        var pdf = WorkOrderPoRenderer.Render(model);

        // Categories on the draft = what the SENT copy should carry, so it self-files: the
        // subcontractor pathway, the order's own record tag, and — when the order came from
        // awarding a tender — the source package's tag (same set as SendWorkOrderPoEmailHandler).
        var categories = new List<string>
        {
            TriageCategories.Marker,
            TriageCategories.Subcontractor,
            TriageCategories.ForRecord(order.Reference)
        };
        if (!string.IsNullOrWhiteSpace(order.BidPackageId))
        {
            var package = await context.BidPackages.FindAsync(new object[] { order.BidPackageId! }, cancellationToken);
            if (package is not null) categories.Add(TriageCategories.ForRecord(package.Reference));
        }

        var created = await mailbox.CreateReplyDraftAsync(
            new MailboxReplyDraftMessage(
                command.MailboxMessageId,
                command.HtmlCoverNote,
                new[] { new MailboxDraftAttachment(model.FileName, "application/pdf", pdf) },
                categories),
            cancellationToken);
        if (created is null)
            throw new InvalidOperationException(
                "The reply draft couldn't be created in the projects mailbox. The original email may "
                + "no longer be there, or the mailbox connection failed — check and try again.");

        // Audit: the drafted reply, with its webLink, so it can be found in Outlook.
        await audit.WriteAsync(
            AuditEventType.DraftCreated,
            $"Purchase order {order.Reference} drafted as a reply in the conversation — awaiting review and send.",
            pathway: "Subcontractor",
            projectId: order.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: order.WorkOrderId,
            recordReference: order.Reference,
            emailMessageId: created.Id,
            webLink: created.WebLink,
            cancellationToken: cancellationToken);

        return new WorkOrderReplyDraft(
            order.WorkOrderId,
            order.Reference,
            created.Subject,
            created.To,
            created.Cc,
            created.WebLink);
    }
}
