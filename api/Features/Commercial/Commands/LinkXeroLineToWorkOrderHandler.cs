using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class LinkXeroLineToWorkOrderHandler : ICommandHandler<LinkXeroLineToWorkOrder, Acknowledgement>
{
    private readonly JpmsContext context;

    public LinkXeroLineToWorkOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(LinkXeroLineToWorkOrder command, CancellationToken cancellationToken)
    {
        var line = await context.XeroLedgerLines.FirstOrDefaultAsync(
            candidate => candidate.XeroLedgerLineId == command.XeroLedgerLineId, cancellationToken);
        if (line is null || !string.Equals(line.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This purchase line is not allocated to this project.");

        if (command.WorkOrderId is not null)
        {
            var workOrderExists = await context.WorkOrders.AnyAsync(
                order => order.WorkOrderId == command.WorkOrderId && order.ProjectId == command.ProjectId, cancellationToken);
            if (!workOrderExists)
                throw new InvalidOperationException("That work order does not exist on this project.");
        }

        line.LinkedWorkOrderId = command.WorkOrderId;
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(line.XeroLedgerLineId);
    }
}
