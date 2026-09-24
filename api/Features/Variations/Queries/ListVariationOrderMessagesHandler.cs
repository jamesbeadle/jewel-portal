using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVariationOrderMessagesHandler
    : IQueryHandler<ListVariationOrderMessages, IReadOnlyList<VariationOrderMessage>>
{
    private readonly JpmsContext context;
    private readonly SignedInCaller caller;
    public ListVariationOrderMessagesHandler(JpmsContext context, SignedInCaller caller)
    { this.context = context; this.caller = caller; }

    public async Task<IReadOnlyList<VariationOrderMessage>> HandleAsync(
        ListVariationOrderMessages query, CancellationToken cancellationToken)
    {
        var isSharedOnly = !caller.MayReadInternalCorrespondence;
        var stored = await context.VariationOrderMessages
            .AsNoTracking()
            .Where(row => row.VariationOrderId == query.VariationOrderId)
            .Where(row => !isSharedOnly || row.Visibility == (int)MessageVisibility.Shared)
            .OrderBy(row => row.PostedAt)
            .ToListAsync(cancellationToken);
        return stored.Select(row => row.ToModel()).ToList().AsReadOnly();
    }
}
