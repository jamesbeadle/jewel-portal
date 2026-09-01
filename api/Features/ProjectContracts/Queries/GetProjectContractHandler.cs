using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Queries;

public sealed class GetProjectContractHandler : IQueryHandler<GetProjectContract, ProjectContract?>
{
    private readonly JpmsContext context;

    public GetProjectContractHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<ProjectContract?> HandleAsync(GetProjectContract query, CancellationToken cancellationToken)
    {
        var entity = await context.ProjectContracts
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProjectId == query.ProjectId, cancellationToken);

        // Null is a real answer — "no contract recorded" — and the client's Nothing<T>() turns a 204
        // into null for a single record. Anything reading terms must handle it rather than assume.
        return entity?.ToModel();
    }
}
