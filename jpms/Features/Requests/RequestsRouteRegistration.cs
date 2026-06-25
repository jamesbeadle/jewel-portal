using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Requests;

public static class RequestsRouteRegistration
{
    public static IServiceCollection AddRequestsReadModels(this IServiceCollection services)
    {
        services.AddScoped<RequestsReadModel>();
        return services;
    }

    public static void RegisterRequestsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListRequestsForProject, IReadOnlyList<Request>>(
            new QueryRoute("/api/projects/{projectId}/requests",
                query => $"/api/projects/{((ListRequestsForProject)query).ProjectId}/requests"));

        queries.Register<GetRequestById, Request?>(
            new QueryRoute("/api/requests/{requestId}",
                query => $"/api/requests/{((GetRequestById)query).RequestId}"));

        commands.Register<RaiseRequest, Request>(
            new CommandRoute("POST", "/api/projects/{projectId}/requests",
                command => $"/api/projects/{((RaiseRequest)command).ProjectId}/requests"));

        commands.Register<UpdateRequestDetails, Request>(
            new CommandRoute("PUT", "/api/requests/{requestId}",
                command => $"/api/requests/{((UpdateRequestDetails)command).RequestId}"));
    }
}
