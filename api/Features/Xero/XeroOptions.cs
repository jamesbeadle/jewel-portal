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
    /// Scopes requested on the token. Must be a subset of the scopes granted to the custom
    /// connection in the Xero developer portal; reading invoices needs accounting.transactions.read.
    /// </summary>
    public string Scopes { get; set; } = "accounting.transactions.read";

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
