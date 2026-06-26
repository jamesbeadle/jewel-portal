using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class AddValuationLineItemHandler : ICommandHandler<AddValuationLineItem, ValuationLineItem>
{
    private readonly JpmsContext context;
    public AddValuationLineItemHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationLineItem> HandleAsync(AddValuationLineItem command, CancellationToken cancellationToken)
    {
        var entity = new ValuationLineItemEntity
        {
            ValuationLineItemId = CommercialIdentifierFactory.NextValuationLineItemId(),
            ProjectId = command.ProjectId,
            ElementType = (int)command.ElementType,
            SectionCode = command.SectionCode ?? "",
            SectionName = command.SectionName ?? "",
            VariationRef = command.VariationRef ?? "",
            VariationTitle = command.VariationTitle ?? "",
            LineType = (int)command.LineType,
            CostCode = command.CostCode ?? "",
            Description = command.Description ?? "",
            Unit = command.Unit ?? "",
            Quantity = command.Quantity,
            Rate = command.Rate,
            LineAmount = ValuationCalculations.LineAmount(command.LineType, command.Quantity, command.Rate),
            Comments = command.Comments ?? "",
            DisplayOrder = command.DisplayOrder
        };
        context.ValuationLineItems.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
