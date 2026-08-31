using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// Configuration for the Bluebeam Studio integration. The client id/secret come from the Bluebeam
/// Developer Portal app ("JBB Portal") and are secrets — app settings / Key Vault only, never
/// source. Bind from the "Bluebeam" section (app-setting names use the double-underscore form,
/// e.g. Bluebeam__ClientId — the names docs/ai/00-agent-architecture.md promised). Defaults are
/// the UK region; RedirectUri must ALSO be registered on the app in the developer portal, or
/// Bluebeam refuses the consent redirect.
/// </summary>
public sealed class BluebeamOptions
{
    public const string SectionName = "Bluebeam";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    // Bluebeam's consent page lives on the US host for every region; tokens and API calls go to
    // the regional host. Both overridable for the day that changes.
    public string AuthorizeUrl { get; set; } = "https://api.bluebeam.com/oauth2/authorize";
    public string ApiBaseUrl { get; set; } = "https://api.bluebeamstudio.co.uk";

    public string RedirectUri { get; set; } = "https://portal.jewelbb.co.uk/api/bluebeam/callback";

    // offline_access is what earns the refresh token; full_user covers the connected account's
    // sessions; jobs covers automated processing.
    public string Scopes { get; set; } = "full_user jobs offline_access";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public static BluebeamOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new BluebeamOptions();
        configuration.GetSection(SectionName).Bind(options);
        return options;
    }
}
