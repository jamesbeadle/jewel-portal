using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Configuration for outbound calls to the Anthropic (Claude) API. The API key is a secret — it is
/// read from app settings / Key Vault only and must never be committed to source. Bind from the
/// "Anthropic" section (app-setting names use the double-underscore form, e.g. Anthropic__ApiKey).
/// </summary>
public sealed class AnthropicOptions
{
    // Default model for request suggestion. Overridable via app setting so the exact model id can be
    // changed without a code change/redeploy if the published alias differs.
    public const string DefaultModel = "claude-sonnet-4-6";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = DefaultModel;

    // Anthropic's required API version header value.
    public string ApiVersion { get; set; } = "2023-06-01";

    // Ceiling on the response size for a single suggestion. Field extraction is small, so this is
    // deliberately modest to bound cost/latency.
    public int MaxTokens { get; set; } = 1024;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public static AnthropicOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Anthropic");
        var options = new AnthropicOptions
        {
            ApiKey = section["ApiKey"],
        };

        var model = section["Model"];
        if (!string.IsNullOrWhiteSpace(model))
            options.Model = model;

        var apiVersion = section["ApiVersion"];
        if (!string.IsNullOrWhiteSpace(apiVersion))
            options.ApiVersion = apiVersion;

        if (int.TryParse(section["MaxTokens"], out var maxTokens) && maxTokens > 0)
            options.MaxTokens = maxTokens;

        return options;
    }
}
