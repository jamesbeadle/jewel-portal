using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class ListLeadsInPipelineHandler
    : IQueryHandler<ListLeadsInPipeline, IReadOnlyList<Lead>>
{
    private readonly JpmsContext context;

    public ListLeadsInPipelineHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Lead>> HandleAsync(
        ListLeadsInPipeline query, CancellationToken cancellationToken)
    {
        var entities = await context.Leads.AsNoTracking().OrderByDescending(lead => lead.CapturedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
