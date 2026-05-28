using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Mobilisation.Commands;
using Jewel.JPMS.Api.Features.Mobilisation.Queries;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Mobilisation;

public static class MobilisationFeatureRegistration
{
    public static IServiceCollection AddMobilisationFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetMobilisationChecklistForProject, MobilisationChecklist>, GetMobilisationChecklistForProjectHandler>();
        services.AddScoped<ICommandHandler<UpdateMobilisationChecklistItem, MobilisationItem>, UpdateMobilisationChecklistItemHandler>();
        services.AddScoped<UpdateMobilisationChecklistItemAuthorisation>();
        services.AddScoped<UpdateMobilisationChecklistItemValidation>();
        return services;
    }
}
