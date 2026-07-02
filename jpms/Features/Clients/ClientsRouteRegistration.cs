using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Clients;

public static class ClientsRouteRegistration
{
    public static IServiceCollection AddClientsReadModels(this IServiceCollection services)
    {
        services.AddScoped<ClientsReadModel>();
        return services;
    }

    public static void RegisterClientsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListClients, IReadOnlyList<Client>>(
            new QueryRoute("/api/clients", _ => "/api/clients"));

        queries.Register<GetClientById, Client?>(
            new QueryRoute("/api/clients/{clientId}",
                query => $"/api/clients/{((GetClientById)query).ClientId}"));

        commands.Register<CreateClient, Client>(
            new CommandRoute("POST", "/api/clients", _ => "/api/clients"));

        commands.Register<UpdateClientContact, Client>(
            new CommandRoute("PUT", "/api/clients/{clientId}/contact",
                command => $"/api/clients/{((UpdateClientContact)command).ClientId}/contact"));
    }
}
