using Jewel.JPMS.Contracts.Cashflow;

namespace Jewel.JPMS.Api.Features.Cashflow.Queries;

public sealed class GetLatestCashflowSnapshotHandler
    : IQueryHandler<GetLatestCashflowSnapshot, CashflowSnapshot?>
{
    private readonly JpmsContext context;

    public GetLatestCashflowSnapshotHandler(JpmsContext context) { this.context = context; }

    public async Task<CashflowSnapshot?> HandleAsync(
        GetLatestCashflowSnapshot query, CancellationToken cancellationToken)
    {
        var entity = await context.CashflowSnapshots.AsNoTracking()
            .OrderByDescending(snapshot => snapshot.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return entity?.ToModel();
    }
}
