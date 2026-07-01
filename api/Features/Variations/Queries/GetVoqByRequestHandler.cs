using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVoqByRequestHandler : IQueryHandler<GetVoqByRequest, VariationOrderQuote?>
{
    private readonly JpmsContext context;
    public GetVoqByRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote?> HandleAsync(GetVoqByRequest query, CancellationToken cancellationToken)
    {
        var entity = await context.VariationOrderQuotes
            .Where(voq => voq.RequestId == query.RequestId)
            .OrderByDescending(voq => voq.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity?.ToModel();
    }
}
