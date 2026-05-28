using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateBidPackageHandler
    : ICommandHandler<CreateBidPackage, BidPackage>
{
    private readonly JpmsContext context;

    public CreateBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage> HandleAsync(CreateBidPackage command, CancellationToken cancellationToken)
    {
        var entity = new BidPackageEntity
        {
            BidPackageId = ProcurementIdentifierFactory.NextBidPackageId(),
            ProjectId = command.ProjectId,
            Title = command.Title,
            Trade = command.Trade,
            Status = (int)BidPackageStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            OwnerEmail = command.OwnerEmail
        };
        context.BidPackages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
