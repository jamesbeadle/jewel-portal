using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class ListDefectsForProjectHandler : IQueryHandler<ListDefectsForProject, IReadOnlyList<Defect>>
{
    private readonly JpmsContext context;
    public ListDefectsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Defect>> HandleAsync(ListDefectsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Defects.Where(d => d.ProjectId == query.ProjectId).OrderByDescending(d => d.RaisedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
