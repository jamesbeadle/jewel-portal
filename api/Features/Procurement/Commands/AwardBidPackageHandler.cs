using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Creates the work order (the purchase-order record) for the winning subcontractor, marks their
// recipient row Won (a previously-marked winner drops back to Responded on re-award), and moves
// the package to Awarded. The order is raised WITH priced lines: the winning quote's lines are
// grouped by the cost code of the package line they price, so cost-centre totals pick the award
// up immediately; any balance of the tender sum not priced against a package line (lump-sum
// extras, attendances) lands on a "Balance of tender sum" line under the package's dominant code,
// recodeable afterwards via RecodeWorkOrderLine.
public sealed class AwardBidPackageHandler
    : ICommandHandler<AwardBidPackage, WorkOrder>
{
    private readonly JpmsContext context;

    public AwardBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(AwardBidPackage command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        // Numbers are sequential per project (mirroring Buildertrend's per-job PO numbering, which
        // seeded orders keep so paperwork cross-references hold), shared across all orders however
        // they were raised (award, direct, seed) — so max+1 within the project, not a count.
        var nextNumber = (await context.WorkOrders
            .Where(order => order.ProjectId == command.ProjectId)
            .MaxAsync(order => (int?)order.Number, cancellationToken) ?? 0) + 1;

        var now = DateTimeOffset.UtcNow;
        var entity = new WorkOrderEntity
        {
            WorkOrderId = ProcurementIdentifierFactory.NextWorkOrderId(),
            ProjectId = command.ProjectId,
            BidPackageId = command.BidPackageId,
            SubcontractorId = command.SubcontractorId,
            Value = command.Value,
            Scope = command.Scope,
            AwardedAt = now,
            AwardedByEmail = command.AwardedByEmail,
            Number = nextNumber,
            Title = package.Title,
            Status = (int)WorkOrderStatus.Released,
            CreatedAt = now
        };
        context.WorkOrders.Add(entity);
        await AddCostCodedLinesAsync(entity, command, cancellationToken);
        package.Status = (int)BidPackageStatus.Awarded;

        var recipients = await context.BidPackageRecipients
            .Where(r => r.BidPackageId == command.BidPackageId)
            .ToListAsync(cancellationToken);
        foreach (var recipient in recipients)
        {
            if (string.Equals(recipient.SubcontractorId, command.SubcontractorId, StringComparison.OrdinalIgnoreCase))
                recipient.Status = (int)BidPackageRecipientStatus.Won;
            else if (recipient.Status == (int)BidPackageRecipientStatus.Won)
                recipient.Status = (int)BidPackageRecipientStatus.Responded;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // Builds the order's priced lines from the winning tender, one line per cost centre:
    //   - quote lines that price a package line contribute their Total to that line's cost code;
    //   - whatever remains of the awarded Value (unmatched quote lines, no quote lines at all,
    //     rounding) becomes a "Balance of tender sum" line on the dominant (largest-allocation)
    //     code, falling back to the package's first line's code.
    // Legacy packages whose lines predate the cost-code rule may have no coded lines at all — the
    // order is then raised without lines, exactly as before the rule landed.
    private async Task AddCostCodedLinesAsync(WorkOrderEntity order, AwardBidPackage command, CancellationToken cancellationToken)
    {
        var packageLines = await context.BidPackageLineItems
            .Where(item => item.BidPackageId == command.BidPackageId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        var codeByLineId = packageLines
            .Where(item => !string.IsNullOrWhiteSpace(item.CostCode))
            .ToDictionary(item => item.LineItemId, item => item.CostCode.Trim(), StringComparer.OrdinalIgnoreCase);
        if (codeByLineId.Count == 0) return;

        // The winning submission: the awarded quote when the caller recorded it, else the
        // subcontractor's latest non-declined quote on the package.
        var quoteId = command.QuoteId;
        if (string.IsNullOrWhiteSpace(quoteId))
            quoteId = (await context.Quotes
                .Where(q => q.BidPackageId == command.BidPackageId
                    && q.SubcontractorId == command.SubcontractorId
                    && !q.IsDeclined)
                .OrderByDescending(q => q.ReceivedAt)
                .FirstOrDefaultAsync(cancellationToken))?.QuoteId;

        // Allocation per cost code from the quote lines that price a package line.
        var allocations = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (quoteId is not null)
        {
            var quoteLines = await context.QuoteLineItems
                .Where(line => line.QuoteId == quoteId && line.BidPackageLineItemId != null)
                .ToListAsync(cancellationToken);
            foreach (var line in quoteLines)
            {
                if (!codeByLineId.TryGetValue(line.BidPackageLineItemId!, out var code)) continue;
                allocations[code] = allocations.GetValueOrDefault(code) + line.Total;
            }
        }

        // Cost centre names make the lines read like the Financials tab; fall back to the code.
        var nameByCode = (await context.CostCenters.ToListAsync(cancellationToken))
            .GroupBy(centre => centre.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);

        var sortOrder = 0;
        void AddLine(string code, string title, decimal amount) =>
            context.WorkOrderLines.Add(new WorkOrderLineEntity
            {
                WorkOrderLineId = ProcurementIdentifierFactory.NextWorkOrderLineId(),
                WorkOrderId = order.WorkOrderId,
                Title = title.Length > 256 ? title[..256] : title,
                Description = $"Awarded from tender {order.Reference} scope",
                CostType = "Subcontractor",
                CostCode = code,
                Quantity = 1m,
                Unit = "item",
                UnitCost = amount,
                LineTotal = amount,
                PaidToDate = 0m,
                SortOrder = sortOrder++
            });

        foreach (var allocation in allocations.OrderByDescending(pair => pair.Value))
            AddLine(allocation.Key, nameByCode.GetValueOrDefault(allocation.Key, allocation.Key), allocation.Value);

        // Balance of the tender sum not priced against a package line.
        var remainder = command.Value - allocations.Values.Sum();
        if (remainder != 0m)
        {
            var dominantCode = allocations.Count > 0
                ? allocations.OrderByDescending(pair => pair.Value).First().Key
                : codeByLineId[packageLines.First(item => codeByLineId.ContainsKey(item.LineItemId)).LineItemId];
            AddLine(dominantCode, "Balance of tender sum", remainder);
        }
    }
}
