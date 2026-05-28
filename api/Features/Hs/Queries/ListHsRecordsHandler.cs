using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Hs.Queries;

public sealed class ListHsRecordsHandler : IQueryHandler<ListHsRecords, IReadOnlyList<HsRecord>>
{
    private readonly JpmsContext context;
    public ListHsRecordsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<HsRecord>> HandleAsync(ListHsRecords query, CancellationToken cancellationToken)
    {
        var entities = await context.HsRecords.OrderByDescending(record => record.RaisedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
