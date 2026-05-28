using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Projects.Queries;

public sealed class ListProjectsVisibleToUserHandler
    : IQueryHandler<ListProjectsVisibleToUser, IReadOnlyList<Project>>
{
    private readonly JpmsContext context;

    public ListProjectsVisibleToUserHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Project>> HandleAsync(
        ListProjectsVisibleToUser query, CancellationToken cancellationToken)
    {
        var entities = await context.Projects
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
