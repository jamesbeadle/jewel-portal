using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Registers the Google Places client used to find local subcontractors near a project. Real client
/// when an API key is present in configuration, otherwise a no-op so the rest of the app runs
/// unchanged and the search UI explains that the key is missing. The key is read from app settings /
/// Key Vault only (GooglePlaces__ApiKey) — never from source control.
/// </summary>
public static class PlacesFeatureRegistration
{
    public static IServiceCollection AddPlacesFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = GooglePlacesOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instance so it doesn't clash with the Graph client's registration.
            services.AddSingleton<IGooglePlacesClient>(sp =>
                new GooglePlacesClient(new HttpClient(), options, sp.GetRequiredService<ILogger<GooglePlacesClient>>()));
        }
        else
        {
            services.AddSingleton<IGooglePlacesClient, NullGooglePlacesClient>();
        }

        return services;
    }
}
