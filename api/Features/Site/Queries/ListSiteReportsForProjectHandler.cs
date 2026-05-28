using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Queries;

public sealed class ListSiteReportsForProjectHandler : IQueryHandler<ListSiteReportsForProject, IReadOnlyList<SiteReport>>
{
    private readonly JpmsContext context;
    public ListSiteReportsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SiteReport>> HandleAsync(ListSiteReportsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.SiteReports.Where(report => report.ProjectId == query.ProjectId).OrderByDescending(report => report.PeriodEnd).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
