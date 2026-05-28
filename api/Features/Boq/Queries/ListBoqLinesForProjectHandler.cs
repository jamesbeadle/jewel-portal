using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Boq.Queries;

public sealed class ListBoqLinesForProjectHandler
    : IQueryHandler<ListBoqLinesForProject, IReadOnlyList<BoqLineItem>>
{
    private readonly JpmsContext context;

    public ListBoqLinesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BoqLineItem>> HandleAsync(
        ListBoqLinesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.BoqLineItems.Where(line => line.ProjectId == query.ProjectId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
