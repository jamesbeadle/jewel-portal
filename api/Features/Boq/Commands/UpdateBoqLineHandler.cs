using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class UpdateBoqLineHandler
    : ICommandHandler<UpdateBoqLine, BoqLineItem>
{
    private readonly JpmsContext context;

    public UpdateBoqLineHandler(JpmsContext context) { this.context = context; }

    public async Task<BoqLineItem> HandleAsync(UpdateBoqLine command, CancellationToken cancellationToken)
    {
        var entity = await context.BoqLineItems.FindAsync(new object[] { command.BoqLineItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"BoQ line {command.BoqLineItemId} not found.");

        entity.Description = command.Description;
        entity.Unit = command.Unit;
        entity.Quantity = command.Quantity;
        entity.RateValue = command.RateValue;
        entity.CostCode = command.CostCode;
        entity.Discipline = (int)command.Discipline;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
