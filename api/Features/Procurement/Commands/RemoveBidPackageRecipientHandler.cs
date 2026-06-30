using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Removes one invited subcontractor from a bid package and returns the remaining recipients.
public sealed class RemoveBidPackageRecipientHandler
    : ICommandHandler<RemoveBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>
{
    private readonly JpmsContext context;

    public RemoveBidPackageRecipientHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageRecipient>> HandleAsync(RemoveBidPackageRecipient command, CancellationToken cancellationToken)
    {
        var recipient = await context.BidPackageRecipients
            .FirstOrDefaultAsync(r => r.BidPackageId == command.BidPackageId && r.RecipientId == command.RecipientId, cancellationToken);
        if (recipient is not null)
        {
            context.BidPackageRecipients.Remove(recipient);
            await context.SaveChangesAsync(cancellationToken);
        }

        var remaining = await context.BidPackageRecipients
            .Where(r => r.BidPackageId == command.BidPackageId)
            .OrderBy(r => r.InvitedAt)
            .ToListAsync(cancellationToken);
        return remaining.Select(e => e.ToModel()).ToList().AsReadOnly();
    }
}
