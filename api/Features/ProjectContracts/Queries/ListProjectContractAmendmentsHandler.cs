using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Queries;

public sealed class ListProjectContractAmendmentsHandler
    : IQueryHandler<ListProjectContractAmendments, IReadOnlyList<ProjectContractAmendment>>
{
    private readonly JpmsContext context;

    public ListProjectContractAmendmentsHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<ProjectContractAmendment>> HandleAsync(
        ListProjectContractAmendments query, CancellationToken cancellationToken)
    {
        // In the order the amendments were made, upload date as the tiebreaker — an undated
        // amendment sorts by when it was filed rather than jumping to either end.
        var entities = await context.ProjectContractAmendments
            .AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(row => row.AmendmentDate ?? row.DocumentUploadedAt)
            .ThenBy(row => row.DocumentUploadedAt)
            .Select(row => row.ToModel())
            .ToList();
    }
}
