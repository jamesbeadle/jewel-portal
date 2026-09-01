using Jewel.JPMS.Api.Features.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Registers the local-business search used to find subcontractors near a project: a Claude web
/// search (the Anthropic web_search server tool) that finds company websites, plus the contact
/// finder that pulls an email/phone off each site. Real search client when the Anthropic API key is
/// present in configuration, otherwise a no-op so the rest of the app runs unchanged and the search
/// UI explains that the key is missing. The key is read from app settings / Key Vault only
/// (Anthropic__ApiKey, shared with the AI feature) — never from source control.
/// </summary>
public static class PlacesFeatureRegistration
{
    public static IServiceCollection AddLocalSearchFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // The same Anthropic key/model the AI feature uses. Read here rather than resolved from DI
        // so this feature does not depend on AddAiFeature's registration order.
        var options = AnthropicOptions.FromConfiguration(configuration);

        if (options.IsConfigured)
        {
            // Own HttpClient instance so it doesn't clash with the Graph client's registration. The
            // search turn runs several web searches inside one call, so it is allowed most of the
            // Static Web Apps gateway's ~45s — contact discovery afterwards has its own budget.
            services.AddSingleton<ILocalBusinessSearch>(sp =>
                new ClaudeLocalBusinessSearch(
                    new HttpClient { Timeout = TimeSpan.FromSeconds(40) },
                    options,
                    sp.GetRequiredService<ILogger<ClaudeLocalBusinessSearch>>()));
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
