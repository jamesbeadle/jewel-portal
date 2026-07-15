using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Appends the supplied line items after the package's existing ones. Existing rows are not read
// for modification, deleted or recreated — their ids, coverage links and quote-line references
// are untouched.
public sealed class AddBidPackageLineItemsHandler
    : ICommandHandler<AddBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>
{
    private readonly JpmsContext context;

    public AddBidPackageLineItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageLineItem>> HandleAsync(AddBidPackageLineItems command, CancellationToken cancellationToken)
    {
        // Every line must land on a centre in the master list (same rule as manual work orders) —
        // a line put out to tender already knows the cost-centre home its committed value takes.
        await context.EnsureCostCodesInMasterAsync(command.LineItems.Select(input => input.CostCode), cancellationToken);

        var maxSortOrder = await context.BidPackageLineItems
            .Where(item => item.BidPackageId == command.BidPackageId)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var order = maxSortOrder + 1;
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
