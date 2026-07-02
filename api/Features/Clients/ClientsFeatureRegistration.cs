using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Clients.Commands;
using Jewel.JPMS.Api.Features.Clients.Queries;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Clients;

public static class ClientsFeatureRegistration
{
    public static IServiceCollection AddClientsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListClients, IReadOnlyList<Client>>, ListClientsHandler>();
        services.AddScoped<IQueryHandler<GetClientById, Client?>, GetClientByIdHandler>();

        services.AddScoped<ICommandHandler<CreateClient, Client>, CreateClientHandler>();
        services.AddScoped<CreateClientAuthorisation>();
        services.AddScoped<CreateClientValidation>();

        services.AddScoped<ICommandHandler<UpdateClientContact, Client>, UpdateClientContactHandler>();
        services.AddScoped<UpdateClientContactAuthorisation>();
        services.AddScoped<UpdateClientContactValidation>();

        return services;
    }
}
