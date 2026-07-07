using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Xero;

/// <summary>
/// Configuration for outbound calls to the Xero Accounting API via a Xero "custom connection"
/// (machine-to-machine, single organisation, OAuth2 client-credentials grant — no user consent
/// redirect). The client id and secret are secrets: they are read from app settings / Key Vault only
/// (app-setting names Xero__ClientId and Xero__ClientSecret) and must never be committed to source.
/// Local development uses local.settings.json (git-ignored).
/// </summary>
public sealed class XeroOptions
{
    public const string SectionName = "Xero";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Optional. When unset (the default) the token request omits the scope parameter and Xero
    /// grants everything the custom connection was set up with — the safe choice, because a
    /// requested scope that doesn't exactly match the granted set fails with invalid_scope
    /// ("Client credentials scope validation failed"). Set only to deliberately narrow the token.
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// Optional. Custom connections are tied to a single organisation so Xero does not require the
    /// xero-tenant-id header; set this only if Xero starts demanding it for the app in use.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>Safety cap on pagination (100 transactions per page) so a huge ledger cannot stall the endpoint.</summary>
    public int MaxPages { get; set; } = 10;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public static XeroOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new XeroOptions
        {
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            TenantId = section["TenantId"]
        };

        var scopes = section["Scopes"];
        if (!string.IsNullOrWhiteSpace(scopes))
            options.Scopes = scopes;

        if (int.TryParse(section["MaxPages"], out var maxPages) && maxPages is > 0 and <= 50)
            options.MaxPages = maxPages;

        return options;
    }
}
