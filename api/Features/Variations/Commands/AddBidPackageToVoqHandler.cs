using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Creates a bid package under a quoting variation order. The package then uses the standard
/// procurement invite/tender/quote flow (recipients, quotes, award). Tendering is all part of the
/// Quoting stage, so the order's status does not change.
/// </summary>
public sealed class AddBidPackageToVoqHandler : ICommandHandler<AddBidPackageToVoq, BidPackage>
{
    private readonly JpmsContext context;
    public AddBidPackageToVoqHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage> HandleAsync(AddBidPackageToVoq command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        var package = new BidPackageEntity
        {
            BidPackageId = VariationsIdentifierFactory.NextBidPackageId(),
            ProjectId = order.ProjectId,
            Title = command.Title,
            Trade = command.Trade,
            Status = (int)BidPackageStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            OwnerEmail = command.OwnerEmail,
            VariationOrderId = order.VariationOrderId
        };
        context.BidPackages.Add(package);

        // Tendering activity stays within the Quoting stage — the order's status does not change.
        await context.SaveChangesAsync(cancellationToken);
        return package.ToModel();
    }
}
