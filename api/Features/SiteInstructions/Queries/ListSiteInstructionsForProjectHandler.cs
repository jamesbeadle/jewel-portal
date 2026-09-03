using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Queries;

public sealed class ListSiteInstructionsForProjectHandler : IQueryHandler<ListSiteInstructionsForProject, IReadOnlyList<SiteInstruction>>
{
    private readonly JpmsContext context;
    public ListSiteInstructionsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SiteInstruction>> HandleAsync(ListSiteInstructionsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.SiteInstructions.AsNoTracking().Where(row => row.ProjectId == query.ProjectId).OrderByDescending(row => row.Number).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
