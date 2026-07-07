using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class ListXeroTransactionsHandler : IQueryHandler<ListXeroTransactions, XeroTransactionsSnapshot>
{
    private readonly IXeroClient xero;

    public ListXeroTransactionsHandler(IXeroClient xero) { this.xero = xero; }

    public Task<XeroTransactionsSnapshot> HandleAsync(ListXeroTransactions query, CancellationToken cancellationToken) =>
        xero.GetPurchaseInvoicesAsync(cancellationToken);
}
