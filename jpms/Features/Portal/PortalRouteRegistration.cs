using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Portal;

public static class PortalRouteRegistration
{
    public static IServiceCollection AddPortalReadModels(this IServiceCollection services)
    {
        services.AddScoped<PortalReadModel>();
        services.AddScoped<PortalWorkOrdersReadModel>();
        services.AddScoped<PortalVariationRequestsReadModel>();
        return services;
    }

    public static void RegisterPortalRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        // The routes are static on purpose: the API resolves WHICH subcontractor from the session
        // cookie (SubcontractorScope), never from the query.
        queries.Register<GetMyPortalRecord, SubcontractorPortalRecord?>(QueryRoute.Static("/api/portal/my/record"));
        queries.Register<ListMyWorkOrders, IReadOnlyList<PortalWorkOrder>>(QueryRoute.Static("/api/portal/my/work-orders"));
        queries.Register<ListMyVariationRequests, IReadOnlyList<SubcontractorVariationRequest>>(QueryRoute.Static("/api/portal/my/variation-requests"));

        commands.Register<RaiseMyVariationRequest, SubcontractorVariationRequest>(
            new CommandRoute("POST", "/api/portal/my/work-orders/{workOrderId}/variation-requests",
                command => $"/api/portal/my/work-orders/{((RaiseMyVariationRequest)command).WorkOrderId}/variation-requests"));

        commands.Register<AcceptMyWorkOrder, WorkOrder>(
            new CommandRoute("POST", "/api/portal/my/work-orders/{workOrderId}/accept",
                command => $"/api/portal/my/work-orders/{((AcceptMyWorkOrder)command).WorkOrderId}/accept"));

        commands.Register<WithdrawMyVariationRequest, SubcontractorVariationRequest>(
            new CommandRoute("POST", "/api/portal/my/variation-requests/{variationRequestId}/withdraw",
                command => $"/api/portal/my/variation-requests/{((WithdrawMyVariationRequest)command).VariationRequestId}/withdraw"));
    }
}
