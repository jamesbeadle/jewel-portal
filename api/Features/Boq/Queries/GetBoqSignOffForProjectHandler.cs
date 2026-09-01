using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Queries;

public sealed class GetBoqSignOffForProjectHandler
    : IQueryHandler<GetBoqSignOffForProject, BoqSignOff?>
{
    private readonly JpmsContext context;

    public GetBoqSignOffForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<BoqSignOff?> HandleAsync(GetBoqSignOffForProject query, CancellationToken cancellationToken)
    {
        var entity = await context.BoqSignOffs.AsNoTracking().FirstOrDefaultAsync(signOff => signOff.ProjectId == query.ProjectId, cancellationToken);
        return entity?.ToModel();
    }
}
