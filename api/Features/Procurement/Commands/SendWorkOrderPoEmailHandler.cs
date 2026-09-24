using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Acceptance;
using Jewel.JPMS.Api.Features.Procurement.Documents;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Emails the purchase order to the supplier from the shared projects mailbox — the one door for
/// every route that sends one: the PO page's "Email to supplier…", the automatic send fired when an
/// order is released, and the tender award's email to the winner. SaveAsDraftOnly stops after
/// staging, leaving the reviewed draft in Drafts for Outlook, which is what the award door used to
/// do for everybody (PrepareWorkOrderEmailDraft, folded in here 2026-09-17 so one email cannot be
/// two emails).
///
/// A draft order is invisible to the supplier by definition and a rejected draft never becomes
/// visible, so both are refused whatever the caller; a supplier without a directory email is
/// refused with the fix in the message. Every email carries the order's acceptance link
/// (2026-09-23): the token is minted here on the first send and re-used after, and the
/// paragraph goes into the composed body on the way out, so no door needs to know the token. The staging, the send, the degrade-to-draft and the audit
/// row are the dispatcher's (OutboundEmailDispatcher), shared with every other record's email.
/// </summary>
public sealed partial class SendWorkOrderPoEmailHandler : ICommandHandler<SendWorkOrderPoEmail, WorkOrderPoEmailOutcome>
{
    private const string StagingRefused =
        "The purchase-order email couldn't be staged in the shared mailbox, so nothing was sent. "
        + "Check the mailbox connection, then email it from the PO page.";

    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;
    private readonly WorkOrderAcceptanceLinks acceptanceLinks;

    public SendWorkOrderPoEmailHandler(JpmsContext context, OutboundEmailDispatcher dispatcher, WorkOrderAcceptanceLinks acceptanceLinks)
    {
        this.context = context; this.dispatcher = dispatcher; this.acceptanceLinks = acceptanceLinks;
    }

    public async Task<WorkOrderPoEmailOutcome> HandleAsync(SendWorkOrderPoEmail command, CancellationToken cancellationToken)
    {
        var order = await ReleasedOrderAsync(command.WorkOrderId, cancellationToken);
        var supplier = await SupplierWithAnEmailAsync(order, cancellationToken);
        var acceptanceLink = await acceptanceLinks.IssuedForAsync(context, order, cancellationToken);

        var message = new MailboxDraftMessage(
            To: new[] { new MailboxDraftRecipient(supplier.ContactEmail!, supplier.CompanyName) },
            Subject: command.Subject,
            HtmlBody: WorkOrderAcceptanceEmailParagraph.InsertInto(command.HtmlBody, acceptanceLink),
            Attachments: new[] { await PurchaseOrderPdfAsync(order.WorkOrderId, cancellationToken) },
            Categories: await CategoriesAsync(order, supplier, cancellationToken));

        var filing = new OutboundEmailFiling(
            CompanyPathways.LabelFor((DirectoryCategory)supplier.Category),
            StagingRefused,
            order.ProjectId,
            RecordType.WorkOrder,
            order.WorkOrderId,
            order.ReferenceOn(await WorkOrderProjectReferences.OfAsync(context, order.ProjectId, cancellationToken)));

        var dispatch = await dispatcher.DispatchAsync(message, filing, command.SaveAsDraftOnly, cancellationToken);
        return new WorkOrderPoEmailOutcome(
            order.WorkOrderId, dispatch.Sent, supplier.ContactEmail!, dispatch.WebLink,
            dispatch.FailureNote, dispatch.MessageId);
    }

    private async Task<WorkOrderEntity> ReleasedOrderAsync(string workOrderId, CancellationToken cancellationToken)
    {
        var order = await context.WorkOrders.FindAsync(new object[] { workOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Work order {workOrderId} not found.");
        if (order.Status == (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException("This work order is still a draft — approve it before emailing the supplier.");
        if (order.Status == (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException("This work order was rejected — it is never sent to the supplier.");
        return order;
    }

    private async Task<SubcontractorEntity> SupplierWithAnEmailAsync(WorkOrderEntity order, CancellationToken cancellationToken)
    {
        var supplier = await context.Subcontractors.FindAsync(new object[] { order.SubcontractorId }, cancellationToken);
        if (supplier is null || string.IsNullOrWhiteSpace(supplier.ContactEmail))
            throw new InvalidOperationException(
                "The supplier has no email address in the directory — add one, then email the purchase order from the PO page.");
        return supplier;
    }
}
