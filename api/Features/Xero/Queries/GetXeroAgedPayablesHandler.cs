using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class GetXeroAgedPayablesHandler : IQueryHandler<GetXeroAgedPayables, XeroAgedPayablesSnapshot>
{
    private readonly IXeroClient xero;

    public GetXeroAgedPayablesHandler(IXeroClient xero) { this.xero = xero; }

    public Task<XeroAgedPayablesSnapshot> HandleAsync(GetXeroAgedPayables query, CancellationToken cancellationToken) =>
        xero.GetAgedPayablesAsync(query.Force, cancellationToken);
}
