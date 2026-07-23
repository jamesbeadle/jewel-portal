using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Revises the value of an APPROVED variation order and moves the commercial records that the
/// approval wrote by the same delta, in one transaction:
///   1. re-prices the Variation line on the Valuation Report (rate + line amount),
///   2. records a delta QS accrual on the CVR (add for an increase, omit for a decrease),
///   3. moves the committed value on the cost-centre budget,
///   4. updates the VariationOrder record itself.
/// Only an approved order has figures to move; before approval, edit the estimate instead.
/// </summary>
public sealed class ReviseVariationOrderValueHandler : ICommandHandler<ReviseVariationOrderValue, VariationOrder>
{
    private readonly JpmsContext context;
    public ReviseVariationOrderValueHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(ReviseVariationOrderValue command, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (entity.Status != (int)VariationOrderStatus.Approved)
            throw new InvalidOperationException("Only an approved variation order can be revised — before approval, edit the estimate.");

        var previousValue = entity.Value;
        var delta = command.Value - previousValue;
        if (delta == 0m) return entity.ToModel();

        var now = DateTimeOffset.UtcNow;

        // 1) Re-price the Variation line on the Valuation Report. These lines are locked against
        //    direct edits (see UpdateValuationLineItemHandler) precisely so they stay in lockstep
        //    with the VO — this is the one place they change.
        var lines = await context.ValuationLineItems
            .Where(line => line.ProjectId == entity.ProjectId
                           && line.ElementType == (int)ValuationElementType.Variation
                           && line.VariationRef == entity.VariationRef)
            .ToListAsync(cancellationToken);
        foreach (var line in lines)
        {
            line.Rate = command.Value;
            line.LineAmount = ValuationCalculations.LineAmount((ValuationLineType)line.LineType, line.Quantity, command.Value);
        }

        // 2) Record the CVR impact as a delta accrual, so the revision history stays on the CVR.
        context.QsAccruals.Add(new QsAccrualEntity
        {
            QsAccrualId = VariationsIdentifierFactory.NextQsAccrualId(),
            ProjectId = entity.ProjectId,
            Category = "Variation",
            Description = $"{entity.VariationRef} — {entity.Title} (revised {Money(previousValue)} → {Money(command.Value)})",
            AddAmount = delta > 0m ? delta : 0m,
            OmitAmount = delta < 0m ? -delta : 0m,
            LiabilityAmount = 0m,
            SignedOffByEmail = command.RevisedByEmail,
            SignedOffAt = now
        });

        // 3) Move the committed value on the cost-centre budget by the same delta (create the
        //    budget row if it has been removed since approval).
        var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
            b => b.ProjectId == entity.ProjectId && b.CostCode == entity.CostCode, cancellationToken);
        if (budget is null)
        {
            budget = new CostCodeBudgetEntity
            {
                CostCodeBudgetId = VariationsIdentifierFactory.NextCostCodeBudgetId(),
                ProjectId = entity.ProjectId,
                CostCode = entity.CostCode ?? "",
                AllocatedAmount = 0m,
                SpentAmount = 0m,
                CommittedAmount = 0m
            };
            context.CostCodeBudgets.Add(budget);
        }
        budget.CommittedAmount += delta;

        // 4) The VO itself.
        entity.Value = command.Value;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string Money(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
}
