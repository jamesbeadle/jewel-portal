using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Contracts.ClientPortal;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

public sealed class ListMyClientVariationOrderMessagesHandler
    : IQueryHandler<ListMyClientVariationOrderMessages, IReadOnlyList<VariationOrderMessage>>
{
    private readonly JpmsContext context;
    public ListMyClientVariationOrderMessagesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<VariationOrderMessage>> HandleAsync(
        ListMyClientVariationOrderMessages query, CancellationToken cancellationToken)
    {
        var isMine = await ClientProjects.OwnsVariationOrderAsync(
            context, query.ClientId, query.VariationOrderId, cancellationToken);
        if (!isMine) return Array.Empty<VariationOrderMessage>();

        // Shared messages only — internal notes stay internal.
        var stored = await context.VariationOrderMessages
            .AsNoTracking()
            .Where(row => row.VariationOrderId == query.VariationOrderId
                && row.Visibility == (int)MessageVisibility.Shared)
            .OrderBy(row => row.PostedAt)
            .ToListAsync(cancellationToken);
        return stored.Select(row => row.ToModel()).ToList().AsReadOnly();
    }
}
