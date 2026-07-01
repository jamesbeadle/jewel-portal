using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVoqByIdHandler : IQueryHandler<GetVoqById, VariationOrderQuote?>
{
    private readonly JpmsContext context;
    public GetVoqByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote?> HandleAsync(GetVoqById query, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrderQuotes.FindAsync(new object[] { query.VariationOrderQuoteId }, cancellationToken);
        return entity?.ToModel();
    }
}
