using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Queries;

public sealed class ListSiteDrawingLinksForProjectHandler
    : IQueryHandler<ListSiteDrawingLinksForProject, IReadOnlyList<SiteDrawingLink>>
{
    private readonly JpmsContext context;

    public ListSiteDrawingLinksForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SiteDrawingLink>> HandleAsync(ListSiteDrawingLinksForProject query, CancellationToken cancellationToken)
    {
        var links = await context.SiteDrawingLinks.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync(cancellationToken);
        return links.Select(link => link.ToModel()).ToList().AsReadOnly();
    }
}
