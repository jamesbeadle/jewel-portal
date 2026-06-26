using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListClaimLinesHandler : IQueryHandler<ListClaimLines, IReadOnlyList<ClaimLine>>
{
    private readonly JpmsContext context;
    public ListClaimLinesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClaimLine>> HandleAsync(ListClaimLines query, CancellationToken cancellationToken)
    {
        var entities = await context.ClaimLines
            .Where(line => line.ValuationClaimId == query.ValuationClaimId)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
