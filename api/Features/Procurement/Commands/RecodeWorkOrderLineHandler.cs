using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Re-codes / splits one work-order line across cost centres. The parts must total the
/// line exactly — this never changes the order's value, only where its committed value
/// sits. One part is a pure recode (quantity and rate preserved); several parts turn the
/// line into per-centre £ lines: the existing line becomes the first part (keeping its
/// id, so invoice links and history stay attached) and the rest are inserted as new
/// lines alongside it. PaidToDate is apportioned pro-rata, last part taking the rounding
/// remainder. Returns the order's full line list after the change.
/// </summary>
public sealed class RecodeWorkOrderLineHandler
    : ICommandHandler<RecodeWorkOrderLine, IReadOnlyList<WorkOrderLine>>
{
    private readonly JpmsContext context;

    public RecodeWorkOrderLineHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<WorkOrderLine>> HandleAsync(RecodeWorkOrderLine command, CancellationToken cancellationToken)
    {
        var line = await context.WorkOrderLines.FindAsync(new object[] { command.WorkOrderLineId }, cancellationToken);
        if (line is null) throw new InvalidOperationException($"Work order line {command.WorkOrderLineId} not found.");

        var order = await context.WorkOrders.FindAsync(new object[] { line.WorkOrderId }, cancellationToken);
        if (order is null || !string.Equals(order.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This line does not belong to a work order on this project.");

        var parts = command.Parts;
        var total = parts.Sum(part => part.Amount);
        if (total != line.LineTotal)
            throw new InvalidOperationException(
                $"The parts total {total:0.00} but the line is {line.LineTotal:0.00} — a recode must account for the whole line exactly.");

        // A split can't flip signs: a negative part on a positive line would inflate the
        // other parts past the line's real value.
        if (parts.Count > 1 && parts.Any(part => Math.Sign(part.Amount) != Math.Sign(line.LineTotal)))
            throw new InvalidOperationException(line.LineTotal < 0m
                ? "This is a negative line — every part must be negative."
                : "Every part must be positive.");

        // Every part must land on a centre in the master list, so the recode can't
        // scatter committed value onto codes the Financials tab treats as legacy.
        var masterCodes = await context.CostCenters
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);
        var masterSet = new HashSet<string>(masterCodes, StringComparer.OrdinalIgnoreCase);
        var unknown = parts.Select(part => part.CostCode)
            .FirstOrDefault(code => !masterSet.Contains(code));
        if (unknown is not null)
            throw new InvalidOperationException($"Cost centre {unknown} is not in the cost-code master.");

        if (parts.Count == 1)
        {
            // Pure recode: the line moves centre wholesale; quantity, rate and paid stay put.
            line.CostCode = parts[0].CostCode;
            await context.SaveChangesAsync(cancellationToken);
            return await LinesOfOrderAsync(line.WorkOrderId, cancellationToken);
        }

        // Split: apportion PaidToDate pro-rata across the parts; the last part takes the
        // rounding remainder so the shares always re-total exactly.
        var originalPaid = line.PaidToDate;
        var originalTotal = line.LineTotal;
        var paidShares = new decimal[parts.Count];
        decimal paidAssigned = 0m;
        for (var index = 0; index < parts.Count; index++)
        {
            paidShares[index] = index == parts.Count - 1
                ? originalPaid - paidAssigned
                : originalTotal == 0m ? 0m : Math.Round(originalPaid * parts[index].Amount / originalTotal, 2);
            paidAssigned += index == parts.Count - 1 ? 0m : paidShares[index];
        }

        // The existing line becomes the first part — same id, so anything pointing at it
        // (splits paid against it, history) stays attached. Priced as a plain £ line: the
        // original quantity/rate pricing no longer describes a partial amount.
        line.CostCode = parts[0].CostCode;
        line.Quantity = 1m;
        line.Unit = "sum";
        line.UnitCost = parts[0].Amount;
        line.LineTotal = parts[0].Amount;
        line.PaidToDate = paidShares[0];

        for (var index = 1; index < parts.Count; index++)
        {
            context.WorkOrderLines.Add(new WorkOrderLineEntity
            {
                WorkOrderLineId = ProcurementIdentifierFactory.NextWorkOrderLineId(),
                WorkOrderId = line.WorkOrderId,
                Title = line.Title,
                Description = line.Description,
                CostType = line.CostType,
                CostCode = parts[index].CostCode,
                LegacyCostCode = line.LegacyCostCode,
                Quantity = 1m,
                Unit = "sum",
                UnitCost = parts[index].Amount,
                LineTotal = parts[index].Amount,
                PaidToDate = paidShares[index],
                SortOrder = line.SortOrder
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return await LinesOfOrderAsync(line.WorkOrderId, cancellationToken);
    }

    private async Task<IReadOnlyList<WorkOrderLine>> LinesOfOrderAsync(string workOrderId, CancellationToken cancellationToken)
    {
        var lines = await context.WorkOrderLines
            .Where(entity => entity.WorkOrderId == workOrderId)
            .OrderBy(entity => entity.SortOrder)
            .ToListAsync(cancellationToken);
        return lines.Select(entity => entity.ToModel()).ToList();
    }
}
