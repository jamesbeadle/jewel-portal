using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Lads.Queries;

public sealed class ListLadClaimsForProjectHandler : IQueryHandler<ListLadClaimsForProject, IReadOnlyList<LadClaim>>
{
    private readonly JpmsContext context;
    public ListLadClaimsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<LadClaim>> HandleAsync(ListLadClaimsForProject query, CancellationToken cancellationToken)
    {
        // Most recently notified first — the newest claim is the one being worked.
        var entities = await context.LadClaims.AsNoTracking()
            .Where(l => l.ProjectId == query.ProjectId)
            .OrderByDescending(l => l.RaisedAt)
            .ThenByDescending(l => l.Number)
            .ToListAsync(cancellationToken);

        return entities.Select(l => l.ToModel()).ToList().AsReadOnly();
    }
}
