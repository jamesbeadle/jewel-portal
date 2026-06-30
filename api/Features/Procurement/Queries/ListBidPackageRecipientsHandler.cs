using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackageRecipientsHandler
    : IQueryHandler<ListBidPackageRecipients, IReadOnlyList<BidPackageRecipient>>
{
    private readonly JpmsContext context;

    public ListBidPackageRecipientsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageRecipient>> HandleAsync(ListBidPackageRecipients query, CancellationToken cancellationToken)
    {
        var entities = await context.BidPackageRecipients
            .Where(recipient => recipient.BidPackageId == query.BidPackageId)
            .OrderBy(recipient => recipient.InvitedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
