using Jewel.JPMS.Contracts.Architects;

namespace Jewel.JPMS.Features.Architects;

public static class ArchitectsRouteRegistration
{
    public static IServiceCollection AddArchitectsReadModels(this IServiceCollection services)
    {
        services.AddScoped<ArchitectsReadModel>();
        return services;
    }

    public static void RegisterArchitectsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListArchitects, IReadOnlyList<Architect>>(
            new QueryRoute("/api/architects", _ => "/api/architects"));

        queries.Register<GetArchitectById, Architect?>(
            new QueryRoute("/api/architects/{architectId}",
                query => $"/api/architects/{((GetArchitectById)query).ArchitectId}"));

        commands.Register<CreateArchitect, Architect>(
            new CommandRoute("POST", "/api/architects", _ => "/api/architects"));

        commands.Register<UpdateArchitect, Architect>(
            new CommandRoute("PUT", "/api/architects/{architectId}",
                command => $"/api/architects/{((UpdateArchitect)command).ArchitectId}"));
    }
}
