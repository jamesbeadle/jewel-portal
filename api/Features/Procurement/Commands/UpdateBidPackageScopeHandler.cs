using Jewel.JPMS.Contracts.Procurement;

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
        entity.MaterialsApplicable = command.MaterialsApplicable;
        if (command.SpecificationSummary is not null)
            entity.SpecificationSummary = command.SpecificationSummary.Length > 4000
                ? command.SpecificationSummary[..4000]
                : command.SpecificationSummary;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
