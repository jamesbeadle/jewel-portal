using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Ends the tender process without a winner: Status → Closed, ClosedAt stamped. Nothing else is
// touched — recipients, tenders and line items stay exactly as they were, so the record reads as
// the audit trail of a tender that ran and ended. An Awarded package can't be closed (the award
// already ended it); closing an already-closed package is a no-op rather than an error.
public sealed class CloseBidPackageHandler : ICommandHandler<CloseBidPackage, BidPackage>
{
    private readonly JpmsContext context;

    public CloseBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<BidPackage> HandleAsync(CloseBidPackage command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        if (package.Status == (int)BidPackageStatus.Awarded)
            throw new InvalidOperationException("An awarded bid package can't be closed — the award already ended the tender.");

        if (package.Status != (int)BidPackageStatus.Closed)
        {
            package.Status = (int)BidPackageStatus.Closed;
            package.ClosedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        return package.ToModel();
    }
}
