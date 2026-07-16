using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Recodes the cost centre of a Variation line on the valuation report. The line's value is
/// locked (it mirrors an approved Variation Order), but where that value sits in the cost-centre
/// master is a finance allocation and may be corrected. To keep the commercial records that were
/// written at approval consistent (see ApproveVariationOrderQuoteHandler), the matching Variation
/// Order is recoded too and its committed value is moved between the cost-centre budgets.
/// </summary>
public sealed class SetValuationLineCostCentreHandler : ICommandHandler<SetValuationLineCostCentre, ValuationLineItem>
{
    private readonly JpmsContext context;
    public SetValuationLineCostCentreHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationLineItem> HandleAsync(SetValuationLineCostCentre command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationLineItems.FindAsync(new object?[] { command.ValuationLineItemId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation line item {command.ValuationLineItemId} was not found.");

        if (entity.ElementType != (int)ValuationElementType.Variation)
            throw new InvalidOperationException(
                "Only variation lines are recoded through this command — edit other lines through the line form.");

        var newCode = command.CostCode.Trim();
        var isKnownCentre = await context.CostCenters.AnyAsync(
            centre => centre.Code == newCode && centre.IsActive, cancellationToken);
        if (!isKnownCentre)
            throw new InvalidOperationException($"'{newCode}' is not an active cost centre.");

        if (entity.CostCode == newCode) return entity.ToModel();

        // Keep the VO mirror and its committed budget in step: approval committed the VO's value
        // against the old centre's budget (and cancellation would release it from vo.CostCode),
        // so the commitment moves with the recode.
        var variationOrder = await context.VariationOrders.FirstOrDefaultAsync(
            vo => vo.ProjectId == entity.ProjectId
                  && vo.VariationRef == entity.VariationRef
                  && vo.Status != (int)VariationOrderStatus.Cancelled,
            cancellationToken);
        if (variationOrder is not null && variationOrder.CostCode != newCode)
        {
            var oldBudget = await context.CostCodeBudgets.FirstOrDefaultAsync(
                b => b.ProjectId == entity.ProjectId && b.CostCode == variationOrder.CostCode, cancellationToken);
            if (oldBudget is not null) oldBudget.CommittedAmount -= variationOrder.Value;

            var newBudget = await context.CostCodeBudgets.FirstOrDefaultAsync(
                b => b.ProjectId == entity.ProjectId && b.CostCode == newCode, cancellationToken);
            if (newBudget is null)
            {
                newBudget = new CostCodeBudgetEntity
                {
                    CostCodeBudgetId = CommercialIdentifierFactory.NextCostCodeBudgetId(),
                    ProjectId = entity.ProjectId,
                    CostCode = newCode,
                    AllocatedAmount = 0m,
                    SpentAmount = 0m,
                    CommittedAmount = 0m
                };
                context.CostCodeBudgets.Add(newBudget);
            }
            newBudget.CommittedAmount += variationOrder.Value;

            variationOrder.CostCode = newCode;
        }

        entity.CostCode = newCode;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
