using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroAgedReceivablesHandler : IQueryHandler<GetXeroAgedReceivables, XeroAgedReceivablesSnapshot>
{
    private readonly IXeroClient xero;

    public GetXeroAgedReceivablesHandler(IXeroClient xero) { this.xero = xero; }

    public Task<XeroAgedReceivablesSnapshot> HandleAsync(GetXeroAgedReceivables query, CancellationToken cancellationToken) =>
        xero.GetAgedReceivablesAsync(query.Force, cancellationToken);
}
