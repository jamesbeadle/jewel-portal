using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Records the winning bid package + subcontractor (and agreed value) on a VOQ and moves it to
/// Selected. The bid package must belong to the VOQ.
/// </summary>
public sealed class SelectVoqTenderHandler : ICommandHandler<SelectVoqTender, VariationOrderQuote>
{
    private readonly JpmsContext context;
    public SelectVoqTenderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote> HandleAsync(SelectVoqTender command, CancellationToken cancellationToken)
    {
        var voq = await context.VariationOrderQuotes.FindAsync(new object[] { command.VariationOrderQuoteId }, cancellationToken);
        if (voq is null) throw new InvalidOperationException($"VOQ {command.VariationOrderQuoteId} not found.");

        var belongs = await context.BidPackages.AnyAsync(
            package => package.BidPackageId == command.BidPackageId
                && package.VariationOrderQuoteId == command.VariationOrderQuoteId,
            cancellationToken);
        if (!belongs) throw new InvalidOperationException("That bid package does not belong to this VOQ.");

        voq.SelectedBidPackageId = command.BidPackageId;
        voq.SelectedSubcontractorId = command.SubcontractorId;
        voq.EstimatedValue = command.EstimatedValue;
        voq.Status = (int)VariationOrderQuoteStatus.Selected;

        await context.SaveChangesAsync(cancellationToken);
        return voq.ToModel();
    }
}
