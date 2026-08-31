using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.ClientPortal.Commands;
using Jewel.JPMS.Api.Features.ClientPortal.Queries;
using Jewel.JPMS.Contracts.ClientPortal;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.ClientPortal;

/// <summary>
/// The client portal: /client-portal/my/* endpoints where the caller is an external client
/// contact and every read/write is scoped to their own ClientId (Gates/ClientScope) — the client
/// twin of the subcontractor portal. Clients follow their projects' RFIs and variation orders and
/// join the SHARED conversation on each; internal notes and email never travel through here.
/// </summary>
public static class ClientPortalFeatureRegistration
{
    public static IServiceCollection AddClientPortalFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListMyClientRequests, IReadOnlyList<ClientPortalRequest>>, ListMyClientRequestsHandler>();
        services.AddScoped<IQueryHandler<GetMyClientRequest, ClientPortalRequest?>, GetMyClientRequestHandler>();
        services.AddScoped<IQueryHandler<ListMyClientRequestMessages, IReadOnlyList<RequestMessage>>, ListMyClientRequestMessagesHandler>();
        services.AddScoped<IQueryHandler<ListMyClientVariationOrders, IReadOnlyList<ClientPortalVariationOrder>>, ListMyClientVariationOrdersHandler>();
        services.AddScoped<IQueryHandler<GetMyClientVariationOrder, ClientPortalVariationOrder?>, GetMyClientVariationOrderHandler>();
        services.AddScoped<IQueryHandler<ListMyClientVariationOrderMessages, IReadOnlyList<VariationOrderMessage>>, ListMyClientVariationOrderMessagesHandler>();

        services.AddScoped<ICommandHandler<PostMyClientRequestMessage, RequestMessage>, PostMyClientRequestMessageHandler>();
        services.AddScoped<ICommandHandler<PostMyClientVariationOrderMessage, VariationOrderMessage>, PostMyClientVariationOrderMessageHandler>();
        return services;
    }
}
