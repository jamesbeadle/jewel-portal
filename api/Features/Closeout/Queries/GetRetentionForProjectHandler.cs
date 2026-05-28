using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class GetRetentionForProjectHandler : IQueryHandler<GetRetentionForProject, RetentionRelease?>
{
    private readonly JpmsContext context;

    public GetRetentionForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<RetentionRelease?> HandleAsync(GetRetentionForProject query, CancellationToken cancellationToken)
    {
        var entity = await context.RetentionReleases
            .FirstOrDefaultAsync(retention => retention.ProjectId == query.ProjectId, cancellationToken);
        return entity?.ToModel();
    }
}
