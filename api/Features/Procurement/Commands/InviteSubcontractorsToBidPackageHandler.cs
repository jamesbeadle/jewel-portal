using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Adds invite rows for the supplied subcontractors (idempotent per package+subcontractor) and moves a
// Draft package to Inviting. Returns the package's full recipient list.
public sealed class InviteSubcontractorsToBidPackageHandler
    : ICommandHandler<InviteSubcontractorsToBidPackage, IReadOnlyList<BidPackageRecipient>>
{
    private readonly JpmsContext context;

    public InviteSubcontractorsToBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageRecipient>> HandleAsync(InviteSubcontractorsToBidPackage command, CancellationToken cancellationToken)
    {
        var existing = await context.BidPackageRecipients
            .Where(recipient => recipient.BidPackageId == command.BidPackageId)
            .ToListAsync(cancellationToken);
        var already = existing.Select(recipient => recipient.SubcontractorId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var added = false;
        foreach (var subcontractorId in command.SubcontractorIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(subcontractorId) || already.Contains(subcontractorId)) continue;
            context.BidPackageRecipients.Add(new BidPackageRecipientEntity
            {
                RecipientId = ProcurementIdentifierFactory.NextRecipientId(),
                BidPackageId = command.BidPackageId,
                SubcontractorId = subcontractorId,
                Status = (int)BidPackageRecipientStatus.Invited,
                InvitedAt = now,
                RespondedAt = null
            });
            already.Add(subcontractorId);
            added = true;
        }

        if (added)
        {
            var package = await context.BidPackages
                .FirstOrDefaultAsync(p => p.BidPackageId == command.BidPackageId, cancellationToken);
            if (package is not null && package.Status == (int)BidPackageStatus.Draft)
                package.Status = (int)BidPackageStatus.Inviting;

            await context.SaveChangesAsync(cancellationToken);
        }

        var all = await context.BidPackageRecipients
            .Where(recipient => recipient.BidPackageId == command.BidPackageId)
            .OrderBy(recipient => recipient.InvitedAt)
            .ToListAsync(cancellationToken);
        return all.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
