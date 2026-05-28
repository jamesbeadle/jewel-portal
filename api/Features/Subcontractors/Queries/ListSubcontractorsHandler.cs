using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListSubcontractorsHandler
    : IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>>
{
    private readonly JpmsContext context;

    public ListSubcontractorsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Subcontractor>> HandleAsync(ListSubcontractors query, CancellationToken cancellationToken)
    {
        var entities = await context.Subcontractors.OrderBy(sub => sub.CompanyName).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
