using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Boq;

public static class BoqRouteRegistration
{
    public static IServiceCollection AddBoqReadModels(this IServiceCollection services)
    {
        services.AddScoped<BoqLinesReadModel>();
        return services;
    }

    public static void RegisterBoqRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListBoqLinesForProject, IReadOnlyList<BoqLineItem>>(
            new QueryRoute("/api/projects/{projectId}/boq",
                query => $"/api/projects/{((ListBoqLinesForProject)query).ProjectId}/boq"));

        queries.Register<GetBoqSignOffForProject, BoqSignOff?>(
            new QueryRoute("/api/projects/{projectId}/boq/sign-off",
                query => $"/api/projects/{((GetBoqSignOffForProject)query).ProjectId}/boq/sign-off"));

        commands.Register<AddBoqLine, BoqLineItem>(
            new CommandRoute("POST", "/api/projects/{projectId}/boq",
                command => $"/api/projects/{((AddBoqLine)command).ProjectId}/boq"));

        commands.Register<UpdateBoqLine, BoqLineItem>(
            new CommandRoute("PUT", "/api/boq-lines/{boqLineItemId}",
                command => $"/api/boq-lines/{((UpdateBoqLine)command).BoqLineItemId}"));

        commands.Register<RemoveBoqLine, Acknowledgement>(
            new CommandRoute("DELETE", "/api/boq-lines/{boqLineItemId}",
                command => $"/api/boq-lines/{((RemoveBoqLine)command).BoqLineItemId}"));

        commands.Register<SignOffBoqForProject, BoqSignOff>(
            new CommandRoute("POST", "/api/projects/{projectId}/boq/sign-off",
                command => $"/api/projects/{((SignOffBoqForProject)command).ProjectId}/boq/sign-off"));
    }
}
