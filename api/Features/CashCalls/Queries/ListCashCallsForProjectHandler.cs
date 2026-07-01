using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CashCalls.Queries;

public sealed class ListCashCallsForProjectHandler : IQueryHandler<ListCashCallsForProject, IReadOnlyList<CashCall>>
{
    private readonly JpmsContext context;
    public ListCashCallsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CashCall>> HandleAsync(ListCashCallsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.CashCalls
            .Where(call => call.ProjectId == query.ProjectId)
            .OrderByDescending(call => call.Number)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
