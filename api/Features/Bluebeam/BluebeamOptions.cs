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

    // EVERY endpoint is regional — authorize included. The support-site auth guide implies one
    // shared consent host (api.bluebeam.com), but that is the US region's: a UK client id sent
    // there lands on the US sign-in server and 400s (learned the hard way, 2026-08-31). The
    // developer portal's own config confirms per-region authorize hosts. Both overridable.
    public string AuthorizeUrl { get; set; } = "https://api.bluebeamstudio.co.uk/oauth2/authorize";
    public string ApiBaseUrl { get; set; } = "https://api.bluebeamstudio.co.uk";

    // The WORKER Function App hosts the callback, not the portal: the Static Web Apps edge
    // intercepts any URL carrying ?code= as one of its own auth callbacks and 500s it, so an
    // OAuth redirect can never safely land on the SWA. Must be registered on the Bluebeam app.
    public string RedirectUri { get; set; } = "https://func-jpms-worker-prod.azurewebsites.net/api/bluebeam/callback";

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
