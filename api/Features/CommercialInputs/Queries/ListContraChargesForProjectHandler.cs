using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListContraChargesForProjectHandler
    : IQueryHandler<ListContraChargesForProject, IReadOnlyList<ContraCharge>>
{
    private readonly JpmsContext context;

    public ListContraChargesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ContraCharge>> HandleAsync(
        ListContraChargesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ContraCharges
            .Where(contra => contra.ProjectId == query.ProjectId)
            .OrderByDescending(contra => contra.RaisedOn)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
