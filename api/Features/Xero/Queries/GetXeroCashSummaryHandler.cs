using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroCashSummaryHandler : IQueryHandler<GetXeroCashSummary, XeroCashSummarySnapshot>
{
    private readonly IXeroClient xero;

    public GetXeroCashSummaryHandler(IXeroClient xero) { this.xero = xero; }

    public Task<XeroCashSummarySnapshot> HandleAsync(GetXeroCashSummary query, CancellationToken cancellationToken) =>
        xero.GetCashSummaryAsync(query.Force, cancellationToken);
}
