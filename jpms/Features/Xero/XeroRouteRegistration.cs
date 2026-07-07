using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Xero;

public static class XeroRouteRegistration
{
    public static IServiceCollection AddXeroReadModels(this IServiceCollection services)
    {
        services.AddScoped<XeroTransactionsReadModel>();
        services.AddScoped<XeroLedgerReadModel>();
        return services;
    }

    public static void RegisterXeroRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListXeroTransactions, XeroTransactionsSnapshot>(
            new QueryRoute("/api/xero/transactions",
                query => ((ListXeroTransactions)query).Force ? "/api/xero/transactions?force=true" : "/api/xero/transactions"));

        queries.Register<ListXeroLedgerLines, IReadOnlyList<XeroLedgerLine>>(QueryRoute.Static("/api/xero/ledger"));

        commands.Register<SyncXeroLedger, XeroLedgerSyncResult>(CommandRoute.Post("/api/xero/ledger/sync"));
        commands.Register<SetXeroAllocation, int>(CommandRoute.Post("/api/xero/allocations"));
    }
}
