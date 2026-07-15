using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Retention.Queries;

public sealed class GetProjectRetentionHandler : IQueryHandler<GetProjectRetention, ProjectRetention?>
{
    private readonly JpmsContext context;

    public GetProjectRetentionHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectRetention?> HandleAsync(GetProjectRetention query, CancellationToken cancellationToken)
    {
        var entity = await context.ProjectRetentions
            .FirstOrDefaultAsync(retention => retention.ProjectId == query.ProjectId, cancellationToken);
        return entity?.ToModel();
    }
}
