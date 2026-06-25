using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Changes.Queries;

public sealed class GetChangeByIdHandler : IQueryHandler<GetChangeById, ChangeRecord?>
{
    private readonly JpmsContext context;

    public GetChangeByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<ChangeRecord?> HandleAsync(GetChangeById query, CancellationToken cancellationToken)
    {
        var entity = await context.ChangeRecords
            .FirstOrDefaultAsync(change => change.ChangeRecordId == query.ChangeRecordId, cancellationToken);
        return entity?.ToModel();
    }
}
