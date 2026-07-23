using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Recodes the cost centre a valuation report line's value sits against. Any element type may be
/// recoded — this moves where the value sits in the cost-centre master (a finance allocation),
/// never the value itself. Contract works, provisional sum and contingency lines simply carry the
/// new code. A Variation line's value is locked (it mirrors an approved Variation Order), so to keep
/// the commercial records written at approval consistent (see ApproveVariationOrderHandler) the
/// matching Variation Order is recoded too and its committed value is moved between the cost-centre
/// budgets.
/// </summary>
public sealed class SetValuationLineCostCentreHandler : ICommandHandler<SetValuationLineCostCentre, ValuationLineItem>
{
    private readonly JpmsContext context;
    public SetValuationLineCostCentreHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationLineItem> HandleAsync(SetValuationLineCostCentre command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationLineItems.FindAsync(new object?[] { command.ValuationLineItemId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation line item {command.ValuationLineItemId} was not found.");

        var newCode = command.CostCode.Trim();
        var isKnownCentre = await context.CostCenters.AnyAsync(
            centre => centre.Code == newCode && centre.IsActive, cancellationToken);
        if (!isKnownCentre)
            throw new InvalidOperationException($"'{newCode}' is not an active cost centre.");

        if (entity.CostCode == newCode) return entity.ToModel();

        // Variation lines mirror an approved VO: keep the VO and its committed budget in step with the
        // recode. Approval committed the VO's value against the old centre's budget, so the
        // commitment moves with the recode. Only an approved VO carries committed budget; other
        // element types (and unapproved variations) just take the new code below.
        if (entity.ElementType == (int)ValuationElementType.Variation)
        {
            var variationOrder = await context.VariationOrders.FirstOrDefaultAsync(
                vo => vo.ProjectId == entity.ProjectId
                      && vo.VariationRef == entity.VariationRef
                      && vo.Status == (int)VariationOrderStatus.Approved,
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
        }

        entity.CostCode = newCode;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
