using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Registers the local-business search used to find subcontractors near a project: a Brave Search
/// API client that finds company websites, plus the contact finder that pulls an email/phone off
/// each site. Real search client when an API key is present in configuration, otherwise a no-op so
/// the rest of the app runs unchanged and the search UI explains that the key is missing. The key
/// is read from app settings / Key Vault only (BraveSearch__ApiKey) — never from source control.
/// </summary>
public static class PlacesFeatureRegistration
{
    public static IServiceCollection AddLocalSearchFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = BraveSearchOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instances so they don't clash with the Graph client's registration.
            services.AddSingleton<ILocalBusinessSearch>(sp =>
                new BraveLocalBusinessSearch(new HttpClient(), options, sp.GetRequiredService<ILogger<BraveLocalBusinessSearch>>()));
        }
        else
        {
            services.AddSingleton<ILocalBusinessSearch, NullLocalBusinessSearch>();
        }

        // Discovers a contact email/phone on each found company's website.
        services.AddSingleton<IWebsiteContactFinder>(sp =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; JPMS/1.0)");
            return new WebsiteContactFinder(http, sp.GetRequiredService<ILogger<WebsiteContactFinder>>());
        });

        return services;
    }
}
