using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

/// <summary>Xero's cash summary with every statement balance a director has keyed laid over it —
/// after Xero's cache, so a figure keyed a moment ago shows at once.</summary>
public sealed class GetXeroCashSummaryHandler : IQueryHandler<GetXeroCashSummary, XeroCashSummarySnapshot>
{
    private readonly IXeroClient xero;
    private readonly JpmsContext context;

    public GetXeroCashSummaryHandler(IXeroClient xero, JpmsContext context)
    {
        this.xero = xero;
        this.context = context;
    }

    public async Task<XeroCashSummarySnapshot> HandleAsync(GetXeroCashSummary query, CancellationToken cancellationToken)
    {
        var snapshot = await xero.GetCashSummaryAsync(query.Force, cancellationToken);
        var keyedRows = await context.KeyedBankStatementBalances.AsNoTracking().ToListAsync(cancellationToken);
        var keyed = keyedRows.Select(row => row.ToModel()).ToList();
        return snapshot.WithKeyedStatements(keyed);
    }
}
