using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class AddBoqLineHandler
    : ICommandHandler<AddBoqLine, BoqLineItem>
{
    private readonly JpmsContext context;

    public AddBoqLineHandler(JpmsContext context) { this.context = context; }

    public async Task<BoqLineItem> HandleAsync(AddBoqLine command, CancellationToken cancellationToken)
    {
        var entity = new BoqLineItemEntity
        {
            BoqLineItemId = BoqIdentifierFactory.NextBoqLineItemId(),
            ProjectId = command.ProjectId,
            Description = command.Description,
            Unit = command.Unit,
            Quantity = command.Quantity,
            RateValue = command.RateValue,
            CostCode = command.CostCode,
            Discipline = (int)command.Discipline
        };
        context.BoqLineItems.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
