using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Queries;

public sealed class ListContractorsReportsHandler : IQueryHandler<ListContractorsReports, IReadOnlyList<ContractorsReport>>
{
    private readonly JpmsContext context;
    public ListContractorsReportsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ContractorsReport>> HandleAsync(ListContractorsReports query, CancellationToken cancellationToken)
    {
        var rows = await context.ContractorsReports.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderByDescending(row => row.PeriodEnd)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}
