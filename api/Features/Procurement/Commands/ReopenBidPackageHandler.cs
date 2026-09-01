using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Puts a closed package back in play, restoring the status its data implies — the same idea as
// undoing a recipient's decline: the record says where the process had got to, so read it back
// from the record rather than remembering. Any live tender → QuotesReceived; any invited
// subcontractor → Inviting; else Draft. ClosedAt is cleared.
public sealed class ReopenBidPackageHandler : ICommandHandler<ReopenBidPackage, BidPackage>
{
    private readonly JpmsContext context;

    public ReopenBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage> HandleAsync(ReopenBidPackage command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        if (package.Status != (int)BidPackageStatus.Closed)
            throw new InvalidOperationException("Only a closed bid package can be reopened.");

        var hasLiveQuote = await context.Quotes
            .AnyAsync(q => q.BidPackageId == command.BidPackageId && !q.IsDeclined, cancellationToken);
        var hasRecipients = await context.BidPackageRecipients
            .AnyAsync(r => r.BidPackageId == command.BidPackageId, cancellationToken);

        package.Status = (int)(hasLiveQuote
            ? BidPackageStatus.QuotesReceived
            : hasRecipients ? BidPackageStatus.Inviting : BidPackageStatus.Draft);
        package.ClosedAt = null;

        await context.SaveChangesAsync(cancellationToken);
        return package.ToModel();
    }
}
