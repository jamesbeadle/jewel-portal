using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class GetSettlementForProjectHandler : IQueryHandler<GetSettlementForProject, SettlementRecord?>
{
    private readonly JpmsContext context;
    public GetSettlementForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<SettlementRecord?> HandleAsync(GetSettlementForProject query, CancellationToken cancellationToken)
    {
        var entity = await context.SettlementRecords.FirstOrDefaultAsync(s => s.ProjectId == query.ProjectId, cancellationToken);
        return entity?.ToModel();
    }
}
