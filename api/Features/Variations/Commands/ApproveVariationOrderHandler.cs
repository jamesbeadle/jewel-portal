using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Approves a variation order — the client's instruction to proceed — writing the value through to
/// the commercial records in one transaction ("Add VO to CVR"):
///   1. mints the V-ref and records the agreed value + cost code on the order,
///   2. Variation line(s) on the Valuation Report (ValuationLineItem, ElementType=Variation),
///   3. a QS accrual on the CVR (add for extra work, omit for a negative/omit variation),
///   4. committed value on the cost-centre budget(s) (CostCodeBudget.CommittedAmount),
/// then marks the order Approved.
///
/// When the command carries a priced build-up (Lines), the value is the sum of the lines, each line
/// is written to the report under its own cost centre, and every cost centre's budget is committed
/// its own share — the approve panel's line-item builder. With no lines the legacy single-line
/// behaviour applies: one Value against one CostCode.
///
/// Approval is allowed from Quoting as well as Issued — the normal route is to issue the priced
/// order to the client and approve on their instruction, but much of the register is historic
/// (seeded) or client-instructed work approved directly with an explicit value and cost code; the
/// write-through is identical either way, so the valuation report, CVR and budget can never fork
/// from how the order was raised. A quoting order with no selected tender simply approves with no
/// subcontractor attached.
/// </summary>
public sealed class ApproveVariationOrderHandler : ICommandHandler<ApproveVariationOrder, VariationOrder>
{
    private readonly JpmsContext context;
    public ApproveVariationOrderHandler(JpmsContext context) { this.context = context; }

    // A line's type follows the sign of its amount — an omit is a negative magnitude.
    private static ValuationLineType LineTypeFor(decimal quantity, decimal rate) =>
        quantity * rate < 0m ? ValuationLineType.Omit : ValuationLineType.Priced;

    public async Task<VariationOrder> HandleAsync(ApproveVariationOrder command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (order.Status == (int)VariationOrderStatus.Approved)
            throw new InvalidOperationException("This variation order is already approved.");
        if (order.Status == (int)VariationOrderStatus.Rejected)
            throw new InvalidOperationException("A rejected variation order cannot be approved.");

        var lines = command.Lines ?? Array.Empty<VariationLineInput>();
        var hasBreakdown = lines.Count > 0;

        // Agreed value: the sum of the priced build-up, or (legacy) the single value / estimate.
        var value = hasBreakdown
            ? lines.Sum(line => ValuationCalculations.LineAmount(LineTypeFor(line.Quantity, line.Rate), line.Quantity, line.Rate))
            : command.Value
                ?? order.EstimatedValue
                ?? throw new InvalidOperationException("A value is required to approve the variation (no estimate is set).");
        if (value == 0m)
            throw new InvalidOperationException("A zero value cannot be approved — enter the agreed value (negative for an omit).");

        // The order carries one cost code (its primary centre). With a build-up that is the first
        // line's centre; the true split lives on the report lines and the per-centre budgets.
        var costCode = hasBreakdown
            ? (string.IsNullOrWhiteSpace(command.CostCode) ? lines[0].CostCode.Trim() : command.CostCode.Trim())
            : command.CostCode.Trim();

        var now = DateTimeOffset.UtcNow;

        // Mint the V-ref against the order's own number (VOQ-0072 → V72) when that ref is still
        // free on THIS project; otherwise take the project's next number. A record returned to
        // quoting and re-approved re-mints here. Numbering is per-project — every project runs its
        // own V-sequence (references like "V18" are only unique within a project).
        var numberFree = order.Number > 0 && !await context.VariationOrders.AnyAsync(
            other => other.ProjectId == order.ProjectId
                     && other.VariationRef == VariationsIdentifierFactory.VariationRef(order.Number)
                     && other.VariationOrderId != order.VariationOrderId, cancellationToken);
        var refNumber = numberFree
            ? order.Number
            : await NextVariationRefNumberAsync(order.ProjectId, cancellationToken);
        var variationRef = VariationsIdentifierFactory.VariationRef(refNumber);

        order.VariationRef = variationRef;
        order.Value = value;
        order.CostCode = costCode;

        // 1) Add the variation to the Valuation Report.
        if (hasBreakdown)
        {
            // The build-up owns this variation's lines: clear any lines already carrying this V-ref
            // (a seeded placeholder, or a re-approval) and write exactly what was priced.
            var priorLines = await context.ValuationLineItems
                .Where(line => line.ProjectId == order.ProjectId
                               && line.ElementType == (int)ValuationElementType.Variation
                               && line.VariationRef == variationRef)
                .ToListAsync(cancellationToken);
            if (priorLines.Count > 0) context.ValuationLineItems.RemoveRange(priorLines);

            var nextDisplayOrder = (await context.ValuationLineItems
                .Where(line => line.ProjectId == order.ProjectId)
                .MaxAsync(line => (int?)line.DisplayOrder, cancellationToken) ?? 0) + 1;

            foreach (var line in lines)
            {
                var lineType = LineTypeFor(line.Quantity, line.Rate);
                context.ValuationLineItems.Add(new ValuationLineItemEntity
                {
                    ValuationLineItemId = VariationsIdentifierFactory.NextValuationLineItemId(),
                    ProjectId = order.ProjectId,
                    ElementType = (int)ValuationElementType.Variation,
                    SectionCode = "",
                    SectionName = "",
                    VariationRef = variationRef,
                    VariationTitle = order.Title,
                    LineType = (int)lineType,
                    CostCode = line.CostCode.Trim(),
                    Description = string.IsNullOrWhiteSpace(line.Description) ? order.Title : line.Description.Trim(),
                    Unit = "item",
                    Quantity = line.Quantity,
                    Rate = line.Rate,
                    LineAmount = ValuationCalculations.LineAmount(lineType, line.Quantity, line.Rate),
                    Comments = $"Variation order {variationRef} (from {order.Reference})",
                    DisplayOrder = nextDisplayOrder++
                });
            }
        }
        else
        {
            // Single line. An omit (negative value) is stored as an Omit line. Seeded history may
            // already carry a display line for this ref (e.g. a TBC placeholder): a single existing
            // line is RE-PRICED rather than duplicated; several existing lines (a VO seeded as split
            // detail lines) already represent the value, so they are left untouched.
            var lineType = value < 0m ? ValuationLineType.Omit : ValuationLineType.Priced;
            var existingLines = await context.ValuationLineItems
                .Where(line => line.ProjectId == order.ProjectId
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
                line.VariationTitle = order.Title;
            }
            else if (existingLines.Count == 0)
            {
                var nextDisplayOrder = (await context.ValuationLineItems
                    .Where(line => line.ProjectId == order.ProjectId)
                    .MaxAsync(line => (int?)line.DisplayOrder, cancellationToken) ?? 0) + 1;
                context.ValuationLineItems.Add(new ValuationLineItemEntity
                {
                    ValuationLineItemId = VariationsIdentifierFactory.NextValuationLineItemId(),
                    ProjectId = order.ProjectId,
                    ElementType = (int)ValuationElementType.Variation,
                    SectionCode = "",
                    SectionName = "",
                    VariationRef = variationRef,
                    VariationTitle = order.Title,
                    LineType = (int)lineType,
                    CostCode = costCode,
                    Description = string.IsNullOrWhiteSpace(order.Description) ? order.Title : order.Description,
                    Unit = "item",
                    Quantity = 1m,
                    Rate = value,
                    LineAmount = ValuationCalculations.LineAmount(lineType, 1m, value),
                    Comments = $"Variation order {variationRef} (from {order.Reference})",
                    DisplayOrder = nextDisplayOrder
                });
            }
            // else: split detail lines already carry this variation's value on the report — leave them.
        }

        // 2) Record the CVR cost impact — an add for extra work, an omit for a reduction. One accrual
        //    for the whole variation regardless of how many report lines it broke into.
        context.QsAccruals.Add(new QsAccrualEntity
        {
            QsAccrualId = VariationsIdentifierFactory.NextQsAccrualId(),
            ProjectId = order.ProjectId,
            Category = "Variation",
            Description = $"{variationRef} — {order.Title}",
            AddAmount = value > 0m ? value : 0m,
            OmitAmount = value < 0m ? -value : 0m,
            LiabilityAmount = 0m,
            SignedOffByEmail = command.ApprovedByEmail,
            SignedOffAt = now
        });

        // 3) Commit the value against the cost-centre budget(s). With a build-up each centre is
        //    committed its own share (grouping the lines by centre); otherwise the whole value goes
        //    against the single cost code. A negative amount releases commitment, mirroring the omit.
        if (hasBreakdown)
        {
            var perCentre = lines
                .GroupBy(line => line.CostCode.Trim())
                .Select(group => (CostCode: group.Key, Amount: group.Sum(line =>
                    ValuationCalculations.LineAmount(LineTypeFor(line.Quantity, line.Rate), line.Quantity, line.Rate))));
            foreach (var centre in perCentre)
                await CommitToBudgetAsync(order.ProjectId, centre.CostCode, centre.Amount, cancellationToken);
        }
        else
        {
            await CommitToBudgetAsync(order.ProjectId, costCode, value, cancellationToken);
        }

        // 4) Advance the order.
        order.Status = (int)VariationOrderStatus.Approved;
        order.ApprovedAt = now;
        order.ApprovedByEmail = command.ApprovedByEmail;
        order.RejectedAt = null;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }

    // Commits an amount against a project cost-centre budget, creating the budget row if none exists.
    private async Task CommitToBudgetAsync(string projectId, string costCode, decimal amount, CancellationToken cancellationToken)
    {
        var budget = await context.CostCodeBudgets.FirstOrDefaultAsync(
            b => b.ProjectId == projectId && b.CostCode == costCode, cancellationToken);
        if (budget is null)
        {
            budget = new CostCodeBudgetEntity
            {
                CostCodeBudgetId = VariationsIdentifierFactory.NextCostCodeBudgetId(),
                ProjectId = projectId,
                CostCode = costCode,
                AllocatedAmount = 0m,
                SpentAmount = 0m,
                CommittedAmount = 0m
            };
            context.CostCodeBudgets.Add(budget);
        }
        budget.CommittedAmount += amount;
    }

    // The next free V-number on the project: one past the highest V-ref already minted. V-refs are
    // "V{n}", so the max is read from the numeric tail of the stored refs.
    private async Task<int> NextVariationRefNumberAsync(string projectId, CancellationToken cancellationToken)
    {
        var refs = await context.VariationOrders
            .Where(o => o.ProjectId == projectId && o.VariationRef != null)
            .Select(o => o.VariationRef!)
            .ToListAsync(cancellationToken);
        var highest = 0;
        foreach (var r in refs)
            if (r.Length > 1 && r[0] == 'V' && int.TryParse(r.AsSpan(1), out var n) && n > highest)
                highest = n;
        return highest + 1;
    }
}
