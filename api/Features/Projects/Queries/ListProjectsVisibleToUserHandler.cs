using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Queries;

public sealed class ListProjectsVisibleToUserHandler
    : IQueryHandler<ListProjectsVisibleToUser, IReadOnlyList<Project>>
{
    private readonly JpmsContext context;

    public ListProjectsVisibleToUserHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Project>> HandleAsync(
        ListProjectsVisibleToUser query, CancellationToken cancellationToken)
    {
        // Newest-first out of the database, then re-sorted into the canonical work order
        // (ProjectOrdering.InWorkOrder: live sites first, Completed last). LINQ's sort is stable,
        // so CreatedAt descending survives as the final tie-break — and the whole app inherits one
        // order from this one query: every picker, the side-nav switcher, the header arrows.
        var entities = await VisibleProjects(query)
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).InWorkOrder().ToList().AsReadOnly();
    }

    private IQueryable<ProjectEntity> VisibleProjects(ListProjectsVisibleToUser query)
    {
        if (query.ArchitectId is { } architectId) return ArchitectProjects.For(context, architectId);
        if (query.ClientId is { } clientId) return ClientProjects.For(context, clientId);
        return context.Projects.AsNoTracking();
    }
}
