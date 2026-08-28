using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Connect;

// The AI connector's OAuth server: dynamic client registration, the authorise/consent/token flow,
// the discovery documents, and the per-user connection list. See docs/ai/10-mcp-connector.md.
public static class ConnectFeatureRegistration
{
    public static IServiceCollection AddConnectFeature(this IServiceCollection services)
    {
        services.AddScoped<OAuthTokenManager>();
        return services;
    }
}
