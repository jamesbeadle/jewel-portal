using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class UpdateValuationLineItemHandler : ICommandHandler<UpdateValuationLineItem, ValuationLineItem>
{
    private readonly JpmsContext context;
    public UpdateValuationLineItemHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationLineItem> HandleAsync(UpdateValuationLineItem command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationLineItems.FindAsync(new object?[] { command.ValuationLineItemId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation line item {command.ValuationLineItemId} was not found.");

        entity.ElementType = (int)command.ElementType;
        entity.SectionCode = command.SectionCode ?? "";
        entity.SectionName = command.SectionName ?? "";
        entity.VariationRef = command.VariationRef ?? "";
        entity.VariationTitle = command.VariationTitle ?? "";
        entity.LineType = (int)command.LineType;
        entity.CostCode = command.CostCode ?? "";
        entity.Description = command.Description ?? "";
        entity.Unit = command.Unit ?? "";
        entity.Quantity = command.Quantity;
        entity.Rate = command.Rate;
        entity.LineAmount = ValuationCalculations.LineAmount(command.LineType, command.Quantity, command.Rate);
        entity.Comments = command.Comments ?? "";
        entity.DisplayOrder = command.DisplayOrder;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
