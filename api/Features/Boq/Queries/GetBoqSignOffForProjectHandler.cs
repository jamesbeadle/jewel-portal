using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Boq.Queries;

public sealed class GetBoqSignOffForProjectHandler
    : IQueryHandler<GetBoqSignOffForProject, BoqSignOff?>
{
    private readonly JpmsContext context;

    public GetBoqSignOffForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<BoqSignOff?> HandleAsync(GetBoqSignOffForProject query, CancellationToken cancellationToken)
    {
        var entity = await context.BoqSignOffs.FirstOrDefaultAsync(signOff => signOff.ProjectId == query.ProjectId, cancellationToken);
        return entity?.ToModel();
    }
}
