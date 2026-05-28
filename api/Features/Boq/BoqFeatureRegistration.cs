using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Boq.Queries;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Boq;

public static class BoqFeatureRegistration
{
    public static IServiceCollection AddBoqFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListBoqLinesForProject, IReadOnlyList<BoqLineItem>>, ListBoqLinesForProjectHandler>();
        services.AddScoped<IQueryHandler<GetBoqSignOffForProject, BoqSignOff?>, GetBoqSignOffForProjectHandler>();

        services.AddScoped<ICommandHandler<AddBoqLine, BoqLineItem>, AddBoqLineHandler>();
        services.AddScoped<AddBoqLineAuthorisation>();
        services.AddScoped<AddBoqLineValidation>();

        services.AddScoped<ICommandHandler<UpdateBoqLine, BoqLineItem>, UpdateBoqLineHandler>();
        services.AddScoped<UpdateBoqLineAuthorisation>();
        services.AddScoped<UpdateBoqLineValidation>();

        services.AddScoped<ICommandHandler<RemoveBoqLine, Acknowledgement>, RemoveBoqLineHandler>();
        services.AddScoped<RemoveBoqLineAuthorisation>();
        services.AddScoped<RemoveBoqLineValidation>();

        services.AddScoped<ICommandHandler<SignOffBoqForProject, BoqSignOff>, SignOffBoqForProjectHandler>();
        services.AddScoped<SignOffBoqForProjectAuthorisation>();
        services.AddScoped<SignOffBoqForProjectValidation>();

        return services;
    }
}
