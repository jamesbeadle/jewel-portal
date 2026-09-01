using Jewel.JPMS.Contracts.Retention;

namespace Jewel.JPMS.Api.Features.Retention.Queries;

public sealed class GetProjectRetentionHandler : IQueryHandler<GetProjectRetention, ProjectRetention?>
{
    private readonly JpmsContext context;

    public GetProjectRetentionHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectRetention?> HandleAsync(GetProjectRetention query, CancellationToken cancellationToken)
    {
        var entity = await context.ProjectRetentions.AsNoTracking()
            .FirstOrDefaultAsync(retention => retention.ProjectId == query.ProjectId, cancellationToken);
        return entity?.ToModel();
    }
}
