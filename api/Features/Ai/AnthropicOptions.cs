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

    // ---- Cost accounting -------------------------------------------------------------------
    // Pence per million tokens, for the agent activity log. Deliberately ZERO by default: the
    // published price changes and a wrong number in a cost column is worse than no number, because
    // somebody will believe it. Set Anthropic__InputPencePerMillion and
    // Anthropic__OutputPencePerMillion from the current price list for the model in use.
    //
    // Token counts are always recorded and are the ground truth; cost is a multiplication over
    // them, computed and frozen at write time so a later rate change cannot rewrite history.
    public decimal InputPencePerMillion { get; set; }
    public decimal OutputPencePerMillion { get; set; }

    public bool IsCostRateConfigured => InputPencePerMillion > 0 || OutputPencePerMillion > 0;

    public decimal CostPence(int inputTokens, int outputTokens) =>
        (inputTokens * InputPencePerMillion / 1_000_000m)
        + (outputTokens * OutputPencePerMillion / 1_000_000m);

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

        if (decimal.TryParse(section["InputPencePerMillion"], out var inputRate) && inputRate >= 0)
            options.InputPencePerMillion = inputRate;

        if (decimal.TryParse(section["OutputPencePerMillion"], out var outputRate) && outputRate >= 0)
            options.OutputPencePerMillion = outputRate;

        return options;
    }
}
