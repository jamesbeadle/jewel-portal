using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListCvrPackagesForProjectHandler
    : IQueryHandler<ListCvrPackagesForProject, IReadOnlyList<CvrPackageRow>>
{
    private readonly JpmsContext context;

    public ListCvrPackagesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CvrPackageRow>> HandleAsync(
        ListCvrPackagesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.CvrPackageRows.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderBy(row => row.PackageName)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
