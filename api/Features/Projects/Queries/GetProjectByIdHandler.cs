using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Projects.Queries;

public sealed class GetProjectByIdHandler
    : IQueryHandler<GetProjectById, Project?>
{
    private readonly JpmsContext context;

    public GetProjectByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<Project?> HandleAsync(GetProjectById query, CancellationToken cancellationToken)
    {
        var entity = await context.Projects.FindAsync(new object[] { query.ProjectId }, cancellationToken);
        if (entity is null) return null;
        return entity.ToModel();
    }
}
