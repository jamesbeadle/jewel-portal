using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Registers the Anthropic (Claude) client used for AI-assisted request suggestion. Real client when
/// an API key is present in configuration, otherwise a no-op so the rest of the app runs unchanged and
/// the triage UI simply falls back to the plain subject/body draft. The key is read from app settings
/// / Key Vault only (Anthropic__ApiKey) — never from source control.
/// </summary>
public static class AiFeatureRegistration
{
    public static IServiceCollection AddAiFeature(this IServiceCollection services, IConfiguration configuration)
    {
        var options = AnthropicOptions.FromConfiguration(configuration);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            // Own HttpClient instance so it doesn't clash with the Graph client's registration.
            services.AddSingleton<IClaudeClient>(sp =>
                new ClaudeClient(new HttpClient(), options, sp.GetRequiredService<ILogger<ClaudeClient>>()));
        }
        else
        {
            services.AddSingleton<IClaudeClient, NullClaudeClient>();
        }

        return services;
    }
}
