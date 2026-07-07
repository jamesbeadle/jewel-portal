using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Xero;

public static class XeroRouteRegistration
{
    public static IServiceCollection AddXeroReadModels(this IServiceCollection services)
    {
        services.AddScoped<XeroTransactionsReadModel>();
        return services;
    }

    public static void RegisterXeroRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListXeroTransactions, XeroTransactionsSnapshot>(
            new QueryRoute("/api/xero/transactions",
                query => ((ListXeroTransactions)query).Force ? "/api/xero/transactions?force=true" : "/api/xero/transactions"));
    }
}
