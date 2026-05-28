using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListLeadsInPipelineHandler
    : IQueryHandler<ListLeadsInPipeline, IReadOnlyList<Lead>>
{
    private readonly JpmsContext context;

    public ListLeadsInPipelineHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Lead>> HandleAsync(
        ListLeadsInPipeline query, CancellationToken cancellationToken)
    {
        var entities = await context.Leads.OrderByDescending(lead => lead.CapturedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
