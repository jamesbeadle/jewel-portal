using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class GetValuationReportSnapshotHandler : IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail>
{
    private readonly JpmsContext context;
    public GetValuationReportSnapshotHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationReportSnapshotDetail> HandleAsync(GetValuationReportSnapshot query, CancellationToken cancellationToken)
    {
        var snapshot = await context.ValuationReportSnapshots
            .FindAsync(new object[] { query.ValuationReportSnapshotId }, cancellationToken);
        if (snapshot is null)
            throw new InvalidOperationException($"Valuation report snapshot {query.ValuationReportSnapshotId} not found.");

        var lines = await context.ValuationReportSnapshotLines.AsNoTracking()
            .Where(line => line.ValuationReportSnapshotId == snapshot.ValuationReportSnapshotId)
            .OrderBy(line => line.DisplayOrder)
            .ToListAsync(cancellationToken);

        return new ValuationReportSnapshotDetail(
            snapshot.ToModel(),
            lines.Select(line => line.ToModel()).ToList());
    }
}
