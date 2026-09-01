using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVariationOrderMessagesHandler
    : IQueryHandler<ListVariationOrderMessages, IReadOnlyList<VariationOrderMessage>>
{
    private readonly JpmsContext context;
    public ListVariationOrderMessagesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<VariationOrderMessage>> HandleAsync(
        ListVariationOrderMessages query, CancellationToken cancellationToken)
    {
        var stored = await context.VariationOrderMessages
            .AsNoTracking()
            .Where(row => row.VariationOrderId == query.VariationOrderId)
            .OrderBy(row => row.PostedAt)
            .ToListAsync(cancellationToken);
        return stored.Select(row => row.ToModel()).ToList().AsReadOnly();
    }
}
