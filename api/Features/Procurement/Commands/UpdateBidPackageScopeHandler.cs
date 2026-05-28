using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateBidPackageScopeHandler
    : ICommandHandler<UpdateBidPackageScope, BidPackage>
{
    private readonly JpmsContext context;

    public UpdateBidPackageScopeHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage> HandleAsync(UpdateBidPackageScope command, CancellationToken cancellationToken)
    {
        var entity = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        entity.Title = command.Title;
        entity.Trade = command.Trade;
        entity.Status = (int)command.Status;
        entity.OwnerEmail = command.OwnerEmail;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
