using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Marks one invited subcontractor as having declined to tender, or undoes that. A decline stamps
// RespondedAt (declining IS their response); undoing restores Responded when they have a live
// quote on the package, otherwise Invited (clearing RespondedAt). Won recipients can't be
// declined — the award has to be revisited first. Returns the package's full recipient list.
public sealed class DeclineBidPackageRecipientHandler
    : ICommandHandler<DeclineBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>
{
    private readonly JpmsContext context;

    public DeclineBidPackageRecipientHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageRecipient>> HandleAsync(DeclineBidPackageRecipient command, CancellationToken cancellationToken)
    {
        var recipient = await context.BidPackageRecipients
            .FirstOrDefaultAsync(r => r.BidPackageId == command.BidPackageId && r.RecipientId == command.RecipientId, cancellationToken);
        if (recipient is null)
            throw new InvalidOperationException($"Recipient {command.RecipientId} not found on bid package {command.BidPackageId}.");
        if (recipient.Status == (int)BidPackageRecipientStatus.Won)
            throw new InvalidOperationException("The winning subcontractor can't be marked as declined — re-award the package first.");

        if (command.Declined)
        {
            recipient.Status = (int)BidPackageRecipientStatus.Declined;
            recipient.RespondedAt ??= DateTimeOffset.UtcNow;
        }
        else if (recipient.Status == (int)BidPackageRecipientStatus.Declined)
        {
            var hasLiveQuote = await context.Quotes
                .AnyAsync(q => q.BidPackageId == command.BidPackageId
                    && q.SubcontractorId == recipient.SubcontractorId
                    && !q.IsDeclined, cancellationToken);
            recipient.Status = (int)(hasLiveQuote ? BidPackageRecipientStatus.Responded : BidPackageRecipientStatus.Invited);
            if (!hasLiveQuote) recipient.RespondedAt = null;
        }

        await context.SaveChangesAsync(cancellationToken);

        var all = await context.BidPackageRecipients
            .Where(r => r.BidPackageId == command.BidPackageId)
            .OrderBy(r => r.InvitedAt)
            .ToListAsync(cancellationToken);
        return all.Select(e => e.ToModel()).ToList().AsReadOnly();
    }
}
