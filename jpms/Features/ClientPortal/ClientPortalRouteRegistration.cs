using Jewel.JPMS.Contracts.ClientPortal;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.ClientPortal;

public static class ClientPortalRouteRegistration
{
    public static IServiceCollection AddClientPortalServices(this IServiceCollection services)
    {
        services.AddScoped<IClientPortalStore, HttpClientPortalStore>();
        return services;
    }

    public static void RegisterClientPortalRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        // List routes are static on purpose: the API resolves WHICH client from the session
        // cookie (ClientScope), never from the query — same shape as the subcontractor portal.
        queries.Register<ListMyClientRequests, IReadOnlyList<ClientPortalRequest>>(
            QueryRoute.Static("/api/client-portal/my/requests"));

        queries.Register<GetMyClientRequest, ClientPortalRequest?>(
            new QueryRoute("/api/client-portal/my/requests/{requestId}",
                query => $"/api/client-portal/my/requests/{((GetMyClientRequest)query).RequestId}"));

        queries.Register<ListMyClientRequestMessages, IReadOnlyList<RequestMessage>>(
            new QueryRoute("/api/client-portal/my/requests/{requestId}/messages",
                query => $"/api/client-portal/my/requests/{((ListMyClientRequestMessages)query).RequestId}/messages"));

        queries.Register<ListMyClientVariationOrders, IReadOnlyList<ClientPortalVariationOrder>>(
            QueryRoute.Static("/api/client-portal/my/variation-orders"));

        queries.Register<GetMyClientVariationOrder, ClientPortalVariationOrder?>(
            new QueryRoute("/api/client-portal/my/variation-orders/{voId}",
                query => $"/api/client-portal/my/variation-orders/{((GetMyClientVariationOrder)query).VariationOrderId}"));

        queries.Register<ListMyClientVariationOrderMessages, IReadOnlyList<VariationOrderMessage>>(
            new QueryRoute("/api/client-portal/my/variation-orders/{voId}/messages",
                query => $"/api/client-portal/my/variation-orders/{((ListMyClientVariationOrderMessages)query).VariationOrderId}/messages"));

        commands.Register<PostMyClientRequestMessage, RequestMessage>(
            new CommandRoute("POST", "/api/client-portal/my/requests/{requestId}/messages",
                command => $"/api/client-portal/my/requests/{((PostMyClientRequestMessage)command).RequestId}/messages"));

        commands.Register<PostMyClientVariationOrderMessage, VariationOrderMessage>(
            new CommandRoute("POST", "/api/client-portal/my/variation-orders/{voId}/messages",
                command => $"/api/client-portal/my/variation-orders/{((PostMyClientVariationOrderMessage)command).VariationOrderId}/messages"));
    }
}
