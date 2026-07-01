using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVoqsForProjectHandler : IQueryHandler<ListVoqsForProject, IReadOnlyList<VariationOrderQuote>>
{
    private readonly JpmsContext context;
    public ListVoqsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<VariationOrderQuote>> HandleAsync(ListVoqsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.VariationOrderQuotes
            .Where(voq => voq.ProjectId == query.ProjectId)
            .OrderByDescending(voq => voq.Number)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
