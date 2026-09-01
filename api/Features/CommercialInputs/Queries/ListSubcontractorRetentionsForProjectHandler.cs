using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CommercialInputs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListSubcontractorRetentionsForProjectHandler
    : IQueryHandler<ListSubcontractorRetentionsForProject, IReadOnlyList<SubcontractorRetention>>
{
    private readonly JpmsContext context;

    public ListSubcontractorRetentionsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SubcontractorRetention>> HandleAsync(
        ListSubcontractorRetentionsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.SubcontractorRetentions.AsNoTracking()
            .Where(retention => retention.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
