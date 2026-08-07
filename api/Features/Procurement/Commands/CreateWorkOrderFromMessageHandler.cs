using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Raises the work order from a tagged email and links the email to it. The order is created by the
// SAME handler as a manually raised order (numbering, draft semantics, cost-code master guard —
// one set of rules, whichever door the order came in through), then the originating email is
// tagged to it through the shared record-link path, exactly like CreateBidPackageFromMessage.
// The order is persisted first because the link path resolves the record from the database;
// a link failure therefore throws with the order already saved — same trade-off as the bid
// package equivalent, and the email stays in the queue to retry against the existing order.
public sealed class CreateWorkOrderFromMessageHandler
    : ICommandHandler<CreateWorkOrderFromMessage, WorkOrder>
{
    private readonly ICommandHandler<CreateManualWorkOrder, WorkOrder> createOrder;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public CreateWorkOrderFromMessageHandler(
        ICommandHandler<CreateManualWorkOrder, WorkOrder> createOrder,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    { this.createOrder = createOrder; this.link = link; }

    public async Task<WorkOrder> HandleAsync(CreateWorkOrderFromMessage command, CancellationToken cancellationToken)
    {
        var order = await createOrder.HandleAsync(
            new CreateManualWorkOrder(
                command.ProjectId,
                command.SubcontractorId,
                command.Title,
                command.Scope,
                command.RaisedByEmail,
                command.Lines,
                command.ProgrammeStart,
                command.TargetCompletion,
                command.ProgrammeNotes,
                SaveAsDraft: command.SaveAsDraft,
                DepositRequired: command.DepositRequired,
                DepositPercent: command.DepositPercent),
            cancellationToken);

        // Tag the originating email to the new order through the shared record-link path (verified by
        // read-back inside the handler). Throws if the email can't be read/tagged.
        await link.HandleAsync(
            new LinkMessageToRecord(command.MessageId, RecordType.WorkOrder, order.WorkOrderId, command.InternetMessageId),
            cancellationToken);

        return order;
    }
}
