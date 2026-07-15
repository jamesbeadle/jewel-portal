using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.Retention.Queries;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Retention;

public static class RetentionFeatureRegistration
{
    public static IServiceCollection AddRetentionFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetProjectRetention, ProjectRetention?>, GetProjectRetentionHandler>();

        services.AddScoped<ICommandHandler<SetProjectRetention, ProjectRetention>, SetProjectRetentionHandler>();
        services.AddScoped<SetProjectRetentionAuthorisation>();
        services.AddScoped<SetProjectRetentionValidation>();

        services.AddScoped<ICommandHandler<ConfirmRetentionRelease, ProjectRetention>, ConfirmRetentionReleaseHandler>();
        services.AddScoped<ConfirmRetentionReleaseAuthorisation>();
        services.AddScoped<ConfirmRetentionReleaseValidation>();

        return services;
    }
}
