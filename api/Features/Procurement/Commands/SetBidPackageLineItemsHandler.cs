using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Replaces all line items on a bid package with the supplied set, preserving order as SortOrder.
public sealed class SetBidPackageLineItemsHandler
    : ICommandHandler<SetBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>
{
    private readonly JpmsContext context;

    public SetBidPackageLineItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageLineItem>> HandleAsync(SetBidPackageLineItems command, CancellationToken cancellationToken)
    {
        // Every line must land on a centre in the master list (same rule as manual work orders) —
        // a line put out to tender already knows the cost-centre home its committed value takes.
        await context.EnsureCostCodesInMasterAsync(command.LineItems.Select(input => input.CostCode), cancellationToken);

        var existing = await context.BidPackageLineItems
            .Where(item => item.BidPackageId == command.BidPackageId)
            .ToListAsync(cancellationToken);
        context.BidPackageLineItems.RemoveRange(existing);

        var order = 0;
        foreach (var input in command.LineItems)
        {
            context.BidPackageLineItems.Add(new BidPackageLineItemEntity
            {
                LineItemId = ProcurementIdentifierFactory.NextLineItemId(),
                BidPackageId = command.BidPackageId,
                Description = input.Description,
                Unit = input.Unit,
                Quantity = input.Quantity,
                Trade = input.Trade,
                CostCode = input.CostCode.Trim(),
                SortOrder = order++
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var stored = await context.BidPackageLineItems
            .Where(item => item.BidPackageId == command.BidPackageId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        return stored.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
