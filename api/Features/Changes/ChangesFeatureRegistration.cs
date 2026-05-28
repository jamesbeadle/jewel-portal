using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Changes.Commands;
using Jewel.JPMS.Api.Features.Changes.Queries;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Changes;

public static class ChangesFeatureRegistration
{
    public static IServiceCollection AddChangesFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListChangesForProject, IReadOnlyList<ChangeRecord>>, ListChangesForProjectHandler>();

        services.AddScoped<ICommandHandler<RaiseChange, ChangeRecord>, RaiseChangeHandler>();
        services.AddScoped<RaiseChangeAuthorisation>();
        services.AddScoped<RaiseChangeValidation>();

        services.AddScoped<ICommandHandler<UpdateChangeDetails, ChangeRecord>, UpdateChangeDetailsHandler>();
        services.AddScoped<UpdateChangeDetailsAuthorisation>();
        services.AddScoped<UpdateChangeDetailsValidation>();

        return services;
    }
}
