using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Changes;

public static class ChangesRouteRegistration
{
    public static IServiceCollection AddChangesReadModels(this IServiceCollection services)
    {
        services.AddScoped<ChangesReadModel>();
        return services;
    }

    public static void RegisterChangesRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListChangesForProject, IReadOnlyList<ChangeRecord>>(
            new QueryRoute("/api/projects/{projectId}/changes",
                query => $"/api/projects/{((ListChangesForProject)query).ProjectId}/changes"));

        commands.Register<RaiseChange, ChangeRecord>(
            new CommandRoute("POST", "/api/projects/{projectId}/changes",
                command => $"/api/projects/{((RaiseChange)command).ProjectId}/changes"));

        commands.Register<UpdateChangeDetails, ChangeRecord>(
            new CommandRoute("PUT", "/api/changes/{changeRecordId}",
                command => $"/api/changes/{((UpdateChangeDetails)command).ChangeRecordId}"));
    }
}
