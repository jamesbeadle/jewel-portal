using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListTradesHandler
    : IQueryHandler<ListTrades, IReadOnlyList<Trade>>
{
    private readonly JpmsContext context;

    public ListTradesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Trade>> HandleAsync(ListTrades query, CancellationToken cancellationToken)
    {
        var entities = await context.Trades.OrderBy(trade => trade.Name).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
