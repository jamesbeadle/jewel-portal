using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;
using Jewel.JPMS.Api.Features.WeeklyCashflow.Queries;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow;

public static class WeeklyCashflowFeatureRegistration
{
    public static IServiceCollection AddWeeklyCashflowFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetWeeklyCashflowPlan, WeeklyCashflowPlan>, GetWeeklyCashflowPlanHandler>();

        services.AddScoped<ICommandHandler<CreateWeeklyCashflowItem, WeeklyCashflowItem>, CreateWeeklyCashflowItemHandler>();
        services.AddScoped<CreateWeeklyCashflowItemAuthorisation>();
        services.AddScoped<CreateWeeklyCashflowItemValidation>();

        services.AddScoped<ICommandHandler<UpdateWeeklyCashflowItem, WeeklyCashflowItem>, UpdateWeeklyCashflowItemHandler>();
        services.AddScoped<UpdateWeeklyCashflowItemAuthorisation>();
        services.AddScoped<UpdateWeeklyCashflowItemValidation>();

        services.AddScoped<ICommandHandler<ArchiveWeeklyCashflowItem, WeeklyCashflowItem>, ArchiveWeeklyCashflowItemHandler>();
        services.AddScoped<ArchiveWeeklyCashflowItemAuthorisation>();

        services.AddScoped<ICommandHandler<PlaceWeeklyCashflowEntry, WeeklyCashflowPlacement?>, PlaceWeeklyCashflowEntryHandler>();
        services.AddScoped<PlaceWeeklyCashflowEntryAuthorisation>();
        services.AddScoped<PlaceWeeklyCashflowEntryValidation>();

        return services;
    }
}
