using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Approves a selected VOQ and raises a Variation Order, writing the value through to the commercial
/// records in one transaction ("Add VO to CVR"):
///   1. a VariationOrder record,
///   2. a Variation line on the Valuation Report (ValuationLineItem, ElementType=Variation),
///   3. a QS accrual on the CVR (QsAccrual add),
///   4. committed value on the cost-centre budget (CostCodeBudget.CommittedAmount),
/// then marks the VOQ Approved. Requires the VOQ to be Selected (a winning tender chosen).
/// </summary>
public sealed class ApproveVariationOrderQuoteHandler : ICommandHandler<ApproveVariationOrderQuote, VariationOrder>
{
    private readonly JpmsContext context;
    public ApproveVariationOrderQuoteHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(ApproveVariationOrderQuote command, CancellationToken cancellationToken)
    {
        var voq = await context.VariationOrderQuotes.FindAsync(new object[] { command.VariationOrderQuoteId }, cancellationToken);
        if (voq is null) throw new InvalidOperationException($"VOQ {command.VariationOrderQuoteId} not found.");
        if (voq.Status != (int)VariationOrderQuoteStatus.Selected)
            throw new InvalidOperationException("A VOQ must have a selected tender before it can be approved.");

        var value = command.Value
            ?? voq.EstimatedValue
            ?? throw new InvalidOperationException("A value is required to approve the VOQ (no estimate is set).");

        var costCode = command.CostCode.Trim();
        var now = DateTimeOffset.UtcNow;

        var nextNumber = (await context.VariationOrders.MaxAsync(vo => (int?)vo.Number, cancellationToken) ?? 0) + 1;
        var variationRef = VariationsIdentifierFactory.VariationRef(nextNumber);

        var variationOrder = new VariationOrderEntity
        {
            VariationOrderId = VariationsIdentifierFactory.NextVariationOrderId(),
            ProjectId = voq.ProjectId,
            VariationOrderQuoteId = voq.VariationOrderQuoteId,
            RequestId = voq.RequestId,
            Number = nextNumber,
            VariationRef = variationRef,
            Title = voq.Title,
            Description = voq.Description,
            Status = (int)VariationOrderStatus.Approved,
            Value = value,
            SubcontractorId = voq.SelectedSubcontractorId,
            CostCode = costCode,
            ApprovedAt = now,
            ApprovedByEmail = command.ApprovedByEmail
        };
        context.VariationOrders.Add(variationOrder);

        // 1) Add VO to the Valuation Report — a Variation line the CVR/PVR reads from the same source.
        var nextDisplayOrder = (await context.ValuationLineItems
            .Where(line => line.ProjectId == voq.ProjectId)
            .MaxAsync(line => (int?)line.DisplayOrder, cancellationToken) ?? 0) + 1;
        context.ValuationLineItems.Add(new ValuationLineItemEntity
        {
            ValuationLineItemId = VariationsIdentifierFactory.NextValuationLineItemId(),
            ProjectId = voq.ProjectId,
            ElementType = (int)ValuationElementType.Variation,
            SectionCode = "",
            SectionName = "",
            VariationRef = variationRef,
            VariationTitle = voq.Title,
            LineType = (int)ValuationLineType.Priced,
            CostCode = costCode,
            Description = string.IsNullOrWhiteSpace(voq.Description) ? voq.Title : voq.Description,
            Unit = "item",
            Quantity = 1m,
            Rate = value,
            LineAmount = value,
            Comments = $"Variation order {variationRef} (from {voq.Reference})",
            DisplayOrder = nextDisplayOrder
        });

        // 2) Record the CVR cost impact — a QS accrual add.
        context.QsAccruals.Add(new QsAccrualEntity
        {
            QsAccrualId = VariationsIdentifierFactory.NextQsAccrualId(),
            ProjectId = voq.ProjectId,
            Category = "Variation",
            Description = $"{variationRef} — {voq.Title}",
            AddAmount = value,
            OmitAmount = 0m,
            LiabilityAmount = 0m,
            SignedOffByEmail = command.ApprovedByEmail,
            SignedOffAt = now
        });

        // 3) Commit the value against the cost-centre budget (create the budget row if none exists).
        var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
            b => b.ProjectId == voq.ProjectId && b.CostCode == costCode, cancellationToken);
        if (budget is null)
        {
            budget = new CostCodeBudgetEntity
            {
                CostCodeBudgetId = VariationsIdentifierFactory.NextCostCodeBudgetId(),
                ProjectId = voq.ProjectId,
                CostCode = costCode,
                AllocatedAmount = 0m,
                SpentAmount = 0m,
                CommittedAmount = 0m
            };
            context.CostCodeBudgets.Add(budget);
        }
        budget.CommittedAmount += value;

        // 4) Advance the VOQ.
        voq.Status = (int)VariationOrderQuoteStatus.Approved;
        voq.ApprovedAt = now;
        voq.ApprovedByEmail = command.ApprovedByEmail;

        await context.SaveChangesAsync(cancellationToken);
        return variationOrder.ToModel();
    }
}
