using Jewel.JPMS.Api.Features.Platform.Commands;
using Jewel.JPMS.Api.Features.Platform.Queries;
using Jewel.JPMS.Contracts.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Platform;

public static class PlatformFeatureRegistration
{
    public static IServiceCollection AddPlatformFeature(this IServiceCollection services)
    {
        // Singleton on purpose: the cache exists to outlive the request scope — see the type.
        services.AddSingleton<AnnouncedVersionCache>();

        services.AddScoped<IQueryHandler<GetAnnouncedAppVersion, AnnouncedAppVersion>, GetAnnouncedAppVersionHandler>();

        services.AddScoped<ICommandHandler<PublishAppVersion, AnnouncedAppVersion>, PublishAppVersionHandler>();
        services.AddScoped<PublishAppVersionAuthorisation>();
        services.AddScoped<PublishAppVersionValidation>();

        return services;
    }
}
