using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Approves a VOQ and raises a Variation Order, writing the value through to the commercial
/// records in one transaction ("Add VO to CVR"):
///   1. a VariationOrder record,
///   2. a Variation line on the Valuation Report (ValuationLineItem, ElementType=Variation),
///   3. a QS accrual on the CVR (add for extra work, omit for a negative/omit variation),
///   4. committed value on the cost-centre budget (CostCodeBudget.CommittedAmount),
/// then marks the VOQ Approved.
///
/// Approval is allowed from any pre-approved status (Draft / Inviting / Tendering / Selected) —
/// not only Selected. The tender pipeline is the normal route, but much of the register is
/// historic (seeded) or client-instructed work that was never tendered through the app; those
/// VOQs are approved MANUALLY with an explicit value and cost code, and the write-through is
/// identical, so the valuation report, CVR and budget can never fork from how the VO was raised.
/// A VOQ with no selected tender simply raises a VO with no subcontractor attached.
/// </summary>
public sealed class ApproveVariationOrderQuoteHandler : ICommandHandler<ApproveVariationOrderQuote, VariationOrder>
{
    private readonly JpmsContext context;
    public ApproveVariationOrderQuoteHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(ApproveVariationOrderQuote command, CancellationToken cancellationToken)
    {
        var voq = await context.VariationOrderQuotes.FindAsync(new object[] { command.VariationOrderQuoteId }, cancellationToken);
        if (voq is null) throw new InvalidOperationException($"VOQ {command.VariationOrderQuoteId} not found.");
        if (voq.Status == (int)VariationOrderQuoteStatus.Approved)
            throw new InvalidOperationException("This VOQ is already approved.");
        if (voq.Status == (int)VariationOrderQuoteStatus.Rejected)
            throw new InvalidOperationException("A rejected VOQ cannot be approved.");

        var value = command.Value
            ?? voq.EstimatedValue
            ?? throw new InvalidOperationException("A value is required to approve the VOQ (no estimate is set).");
        if (value == 0m)
            throw new InvalidOperationException("A zero value cannot be approved — enter the agreed value (negative for an omit).");

        var costCode = command.CostCode.Trim();
        var now = DateTimeOffset.UtcNow;

        // The quote and the order are two stages of the same document, so the VO keeps the VOQ's
        // number (VOQ-0072 → V72) whenever that ref is still free on THIS project; otherwise it
        // takes the project's next number. Numbering is per-project — every project runs its own
        // V-sequence (references like "V18" are only unique within a project).
        var voqNumberFree = voq.Number > 0 && !await context.VariationOrders.AnyAsync(
            vo => vo.ProjectId == voq.ProjectId && vo.Number == voq.Number, cancellationToken);
        var nextNumber = voqNumberFree
            ? voq.Number
            : (await context.VariationOrders
                  .Where(vo => vo.ProjectId == voq.ProjectId)
                  .MaxAsync(vo => (int?)vo.Number, cancellationToken) ?? 0) + 1;
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

        // 1) Add VO to the Valuation Report — a Variation line the CVR/PVR reads from the same
        //    source. An omit (negative value) is stored as an Omit line, matching the register's
        //    convention. Seeded history may already carry a display line for this ref (e.g. a
        //    TBC placeholder): a single existing line is RE-PRICED to the approved value rather
        //    than duplicated; several existing lines (a VO seeded as split detail lines) already
        //    represent the value, so they are left untouched.
        var lineType = value < 0m ? ValuationLineType.Omit : ValuationLineType.Priced;
        var existingLines = await context.ValuationLineItems
            .Where(line => line.ProjectId == voq.ProjectId
                           && line.ElementType == (int)ValuationElementType.Variation
                           && line.VariationRef == variationRef)
            .ToListAsync(cancellationToken);
        if (existingLines.Count == 1)
        {
            var line = existingLines[0];
            line.LineType = (int)lineType;
            line.CostCode = costCode;
            line.Rate = value;
            line.LineAmount = ValuationCalculations.LineAmount(lineType, line.Quantity, value);
            line.VariationTitle = voq.Title;
        }
        else if (existingLines.Count == 0)
        {
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
                LineType = (int)lineType,
                CostCode = costCode,
                Description = string.IsNullOrWhiteSpace(voq.Description) ? voq.Title : voq.Description,
                Unit = "item",
                Quantity = 1m,
                Rate = value,
                LineAmount = ValuationCalculations.LineAmount(lineType, 1m, value),
                Comments = $"Variation order {variationRef} (from {voq.Reference})",
                DisplayOrder = nextDisplayOrder
            });
        }
        // else: split detail lines already carry this VO's value on the report — leave them.

        // 2) Record the CVR cost impact — an add for extra work, an omit for a reduction.
        context.QsAccruals.Add(new QsAccrualEntity
        {
            QsAccrualId = VariationsIdentifierFactory.NextQsAccrualId(),
            ProjectId = voq.ProjectId,
            Category = "Variation",
            Description = $"{variationRef} — {voq.Title}",
            AddAmount = value > 0m ? value : 0m,
            OmitAmount = value < 0m ? -value : 0m,
            LiabilityAmount = 0m,
            SignedOffByEmail = command.ApprovedByEmail,
            SignedOffAt = now
        });

        // 3) Commit the value against the cost-centre budget (create the budget row if none
        //    exists). A negative value releases commitment, mirroring the omit.
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
