using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListValuationClaimsForProjectHandler : IQueryHandler<ListValuationClaimsForProject, IReadOnlyList<ValuationClaim>>
{
    private readonly JpmsContext context;
    public ListValuationClaimsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ValuationClaim>> HandleAsync(ListValuationClaimsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ValuationClaims
            .Where(claim => claim.ProjectId == query.ProjectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
