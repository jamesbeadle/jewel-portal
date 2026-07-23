using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Links a single bid package line item to its commercial home — exactly one of a contract BoQ line
// (so the tendered scope flows into the Programme Valuation Report against the original tender) or a
// Variation Order Quote (extra-over scope priced outside the contract sum). Enforces the one-of rule
// and that the referenced record lives on the same project as the package. Returns the package's full
// ordered line-item list so the caller re-renders with the updated coverage.
public sealed class SetBidPackageLineItemCoverageHandler
    : ICommandHandler<SetBidPackageLineItemCoverage, IReadOnlyList<BidPackageLineItem>>
{
    private readonly JpmsContext context;

    public SetBidPackageLineItemCoverageHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageLineItem>> HandleAsync(SetBidPackageLineItemCoverage command, CancellationToken cancellationToken)
    {
        var lineItem = await context.BidPackageLineItems
            .FirstOrDefaultAsync(item => item.LineItemId == command.LineItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Bid package line item '{command.LineItemId}' not found.");

        var package = await context.BidPackages
            .FirstOrDefaultAsync(p => p.BidPackageId == lineItem.BidPackageId, cancellationToken)
            ?? throw new InvalidOperationException($"Bid package '{lineItem.BidPackageId}' not found.");

        switch (command.Coverage)
        {
            case BidPackageLineCoverage.ContractLine:
            {
                if (string.IsNullOrWhiteSpace(command.BoqLineItemId))
                    throw new InvalidOperationException("A contract-line coverage needs a BoqLineItemId.");
                if (!string.IsNullOrWhiteSpace(command.VariationOrderId))
                    throw new InvalidOperationException("A line item is covered by a BoQ line OR a variation, not both.");

                var boqExists = await context.BoqLineItems.AnyAsync(
                    b => b.BoqLineItemId == command.BoqLineItemId && b.ProjectId == package.ProjectId, cancellationToken);
                if (!boqExists)
                    throw new InvalidOperationException($"BoQ line '{command.BoqLineItemId}' not found on project '{package.ProjectId}'.");

                lineItem.Coverage = (int)BidPackageLineCoverage.ContractLine;
                lineItem.BoqLineItemId = command.BoqLineItemId;
                lineItem.VariationOrderId = null;
                break;
            }
            case BidPackageLineCoverage.Variation:
            {
                if (string.IsNullOrWhiteSpace(command.VariationOrderId))
                    throw new InvalidOperationException("A variation coverage needs a VariationOrderId.");
                if (!string.IsNullOrWhiteSpace(command.BoqLineItemId))
                    throw new InvalidOperationException("A line item is covered by a BoQ line OR a variation, not both.");

                var voqExists = await context.VariationOrders.AnyAsync(
                    v => v.VariationOrderId == command.VariationOrderId && v.ProjectId == package.ProjectId, cancellationToken);
                if (!voqExists)
                    throw new InvalidOperationException($"Variation Order '{command.VariationOrderId}' not found on project '{package.ProjectId}'.");

                lineItem.Coverage = (int)BidPackageLineCoverage.Variation;
                lineItem.VariationOrderId = command.VariationOrderId;
                lineItem.BoqLineItemId = null;
                break;
            }
            case BidPackageLineCoverage.Unassigned:
            {
                // Clearing the link — both ids must be empty.
                if (!string.IsNullOrWhiteSpace(command.BoqLineItemId) || !string.IsNullOrWhiteSpace(command.VariationOrderId))
                    throw new InvalidOperationException("Clearing coverage cannot supply a BoQ line or variation id.");

                lineItem.Coverage = (int)BidPackageLineCoverage.Unassigned;
                lineItem.BoqLineItemId = null;
                lineItem.VariationOrderId = null;
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown coverage '{command.Coverage}'.");
        }

        await context.SaveChangesAsync(cancellationToken);

        var stored = await context.BidPackageLineItems
            .Where(item => item.BidPackageId == lineItem.BidPackageId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        return stored.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
