using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class GetRequestByIdHandler : IQueryHandler<GetRequestById, Request?>
{
    private readonly JpmsContext context;

    public GetRequestByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<Request?> HandleAsync(GetRequestById query, CancellationToken cancellationToken)
    {
        var entity = await context.Requests
            .FirstOrDefaultAsync(change => change.RequestId == query.RequestId, cancellationToken);
        return entity?.ToModel();
    }
}
