using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Architects.Queries;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Architects;

public static class ArchitectsFeatureRegistration
{
    public static IServiceCollection AddArchitectsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListArchitects, IReadOnlyList<Architect>>, ListArchitectsHandler>();
        services.AddScoped<IQueryHandler<GetArchitectById, Architect?>, GetArchitectByIdHandler>();

        services.AddScoped<ICommandHandler<CreateArchitect, Architect>, CreateArchitectHandler>();
        services.AddScoped<CreateArchitectAuthorisation>();
        services.AddScoped<CreateArchitectValidation>();

        services.AddScoped<ICommandHandler<UpdateArchitect, Architect>, UpdateArchitectHandler>();
        services.AddScoped<UpdateArchitectAuthorisation>();
        services.AddScoped<UpdateArchitectValidation>();

        return services;
    }
}
