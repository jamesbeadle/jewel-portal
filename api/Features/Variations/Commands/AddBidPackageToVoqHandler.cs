using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Creates a bid package under a VOQ and advances the VOQ from Draft to Inviting. The package then
/// uses the standard procurement invite/tender/quote flow (recipients, quotes, award).
/// </summary>
public sealed class AddBidPackageToVoqHandler : ICommandHandler<AddBidPackageToVoq, BidPackage>
{
    private readonly JpmsContext context;
    public AddBidPackageToVoqHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage> HandleAsync(AddBidPackageToVoq command, CancellationToken cancellationToken)
    {
        var voq = await context.VariationOrderQuotes.FindAsync(new object[] { command.VariationOrderQuoteId }, cancellationToken);
        if (voq is null) throw new InvalidOperationException($"VOQ {command.VariationOrderQuoteId} not found.");

        var package = new BidPackageEntity
        {
            BidPackageId = VariationsIdentifierFactory.NextBidPackageId(),
            ProjectId = voq.ProjectId,
            Title = command.Title,
            Trade = command.Trade,
            Status = (int)BidPackageStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            OwnerEmail = command.OwnerEmail,
            VariationOrderQuoteId = voq.VariationOrderQuoteId
        };
        context.BidPackages.Add(package);

        if (voq.Status == (int)VariationOrderQuoteStatus.Draft)
            voq.Status = (int)VariationOrderQuoteStatus.Inviting;

        await context.SaveChangesAsync(cancellationToken);
        return package.ToModel();
    }
}
