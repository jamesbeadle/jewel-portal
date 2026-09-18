using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Documents;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Creates an Outlook draft REPLY to an email already linked to the work order, carrying the freshly
// rendered purchase-order PDF. Graph's createReplyAll keeps the reply in the original conversation —
// "RE:" subject, thread headers, quoted history, original recipients — and the caller's cover note
// is placed above the quoted history, so the formal purchase order arrives inside the email chain
// the works were agreed in (the request flow's SendRequestReplyHandler, retold for
// procurement). The draft carries the order's workflow tag (and the source package's, when there is
// one) so the sent copy and the supplier's replies group under the order. Nothing is sent — the
// dispatcher is asked for a draft and stops there, a person reviews, adjusts recipients if needed,
// and sends from the mailbox itself — and unlike the request flow there is NO status side effect: a
// work order's lifecycle is driven by approval and acceptance, never by drafting its covering email.
public sealed partial class PrepareWorkOrderReplyDraftHandler : ICommandHandler<PrepareWorkOrderReplyDraft, WorkOrderReplyDraft>
{
    private const string StagingRefused =
        "The reply draft couldn't be created in the projects mailbox. The original email may "
        + "no longer be there, or the mailbox connection failed — check and try again.";

    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;

    public PrepareWorkOrderReplyDraftHandler(JpmsContext context, OutboundEmailDispatcher dispatcher)
    {
        this.context = context;
        this.dispatcher = dispatcher;
    }

    public async Task<WorkOrderReplyDraft> HandleAsync(PrepareWorkOrderReplyDraft command, CancellationToken cancellationToken)
    {
        var order = await EmailableOrderAsync(command.WorkOrderId, cancellationToken);
        var model = await WorkOrderPoDocumentBuilder.BuildAsync(context, command.WorkOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");
        var bucket = await CompanyPathways.BucketAsync(context, order, cancellationToken);

        var reply = await PurchaseOrderReplyAsync(command, order, model, bucket, cancellationToken);
        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(bucket), StagingRefused,
            order.ProjectId, RecordType.WorkOrder, order.WorkOrderId, order.Reference);

        var dispatch = await dispatcher.DispatchReplyAsync(reply, filing, saveAsDraftOnly: true, cancellationToken);
        return new WorkOrderReplyDraft(
            order.WorkOrderId, order.Reference, dispatch.Subject,
            dispatch.To ?? Array.Empty<string>(), dispatch.Cc ?? Array.Empty<string>(), dispatch.WebLink);
    }

    // Same promises as the fresh-draft and send handlers, kept against direct calls — plus
    // Cancelled, which the PO page keeps un-emailable: a voided order's PO would announce a
    // commitment that no longer stands.
    private async Task<WorkOrderEntity> EmailableOrderAsync(string workOrderId, CancellationToken cancellationToken)
    {
        var order = await context.WorkOrders.FindAsync(new object[] { workOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Work order {workOrderId} not found.");
        if (order.Status == (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException(
                "This work order is still a draft — approve it before emailing the supplier.");
        if (order.Status == (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException(
                "This work order was rejected — it is never sent to the supplier.");
        if (order.Status == (int)WorkOrderStatus.Cancelled)
            throw new InvalidOperationException(
                "This work order was cancelled — its purchase order is a voided record and is never emailed.");
        return order;
    }
}
