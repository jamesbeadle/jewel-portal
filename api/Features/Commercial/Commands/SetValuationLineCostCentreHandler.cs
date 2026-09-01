using System.Globalization;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Recodes the cost centre a valuation report line's value sits against. Any element type may be
/// recoded — this moves where the value sits in the cost-centre master (a finance allocation),
/// never the value itself. Contract works, provisional sum and contingency lines simply carry the
/// new code. A Variation line's value is locked (it mirrors an approved Variation Order), so to keep
/// the commercial records written at approval consistent (see ApproveVariationOrderHandler) the
/// matching Variation Order is recoded too and its committed value is moved between the cost-centre
/// budgets.
///
/// Every recode is written to the audit trail (AuditEventType.CostCentreRecoded) — the finance
/// reconciliation record of who moved which line, from where to where, and the value that moved
/// with it. Per the AuditTrail convention the write happens AFTER the save has succeeded and is
/// best-effort: an audit failure never fails the recode it records.
/// </summary>
public sealed class SetValuationLineCostCentreHandler : ICommandHandler<SetValuationLineCostCentre, ValuationLineItem>
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public SetValuationLineCostCentreHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

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

        var oldCode = entity.CostCode;

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

        // The reconciliation record, after the save so the trail never records a move that didn't
        // commit. Codes are labelled with the master's names where known (the OLD code may since
        // have been retired — it still reads back as its bare code rather than blocking the trail).
        var centreNames = (await context.CostCenters.AsNoTracking()
                .Where(centre => centre.Code == oldCode || centre.Code == newCode)
                .Select(centre => new { centre.Code, centre.Name, centre.IsActive })
                .ToListAsync(cancellationToken))
            .GroupBy(centre => centre.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(centre => centre.IsActive).First().Name,
                StringComparer.OrdinalIgnoreCase);
        string Centre(string code) =>
            centreNames.TryGetValue(code, out var name) && !string.IsNullOrWhiteSpace(name)
                ? $"{code} {name}" : code;

        var isVariation = entity.ElementType == (int)ValuationElementType.Variation;
        var lineLabel = isVariation
            ? $"{entity.VariationRef} {entity.VariationTitle}".Trim()
            : $"{entity.SectionCode} {entity.SectionName}".Trim();
        await audit.WriteAsync(
            AuditEventType.CostCentreRecoded,
            $"{lineLabel} ({entity.LineAmount.ToString("C2", Gb)}) moved from cost centre " +
            $"{Centre(oldCode)} to {Centre(newCode)}.",
            projectId: entity.ProjectId,
            recordType: isVariation ? RecordType.Variation : null,
            recordReference: isVariation ? entity.VariationRef : entity.SectionCode,
            cancellationToken: cancellationToken);

        return entity.ToModel();
    }
}
