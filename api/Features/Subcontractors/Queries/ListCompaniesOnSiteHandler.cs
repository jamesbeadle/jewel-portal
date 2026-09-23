using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListCompaniesOnSiteHandler : IQueryHandler<ListCompaniesOnSite, IReadOnlyList<CompanyOnSite>>
{
    private const int Released = (int)WorkOrderStatus.Released;
    private const int Completed = (int)ProjectStage.Completed;
    private readonly JpmsContext context;

    public ListCompaniesOnSiteHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CompanyOnSite>> HandleAsync(ListCompaniesOnSite query, CancellationToken cancellationToken)
    {
        var orders = await context.WorkOrders.AsNoTracking()
            .Where(order => order.Status == Released && order.SubcontractorId != "")
            .Join(context.Projects.AsNoTracking().Where(project => project.Stage != Completed),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => new { order.SubcontractorId, ProjectName = project.Name })
            .ToListAsync(cancellationToken);
        return orders
            .GroupBy(order => order.SubcontractorId, StringComparer.OrdinalIgnoreCase)
            .Select(company => new CompanyOnSite(company.Key, NamesOf(company.Select(order => order.ProjectName))))
            .ToList();
    }

    private static IReadOnlyList<string> NamesOf(IEnumerable<string> projectNames) =>
        projectNames.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
}
