using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateWorkOrderHandler
    : ICommandHandler<UpdateWorkOrder, WorkOrder>
{
    private readonly JpmsContext context;

    public UpdateWorkOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(UpdateWorkOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");

        entity.Value = command.Value;
        entity.Scope = command.Scope;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
