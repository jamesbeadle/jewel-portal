using Jewel.JPMS.Contracts.Rates;

namespace Jewel.JPMS.Api.Features.Rates.Queries;

public sealed class ListRatesInLibraryHandler
    : IQueryHandler<ListRatesInLibrary, IReadOnlyList<Rate>>
{
    private readonly JpmsContext context;

    public ListRatesInLibraryHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Rate>> HandleAsync(ListRatesInLibrary query, CancellationToken cancellationToken)
    {
        var entities = await context.Rates.AsNoTracking().OrderBy(rate => rate.Trade).ThenBy(rate => rate.Description).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
