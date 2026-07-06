using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Configuration for outbound calls to the Brave Search API — used to find local subcontractors
/// near a project's site. (Brave's terms permit this use; Google Places' do not, which is why the
/// feature moved.) The API key is a secret: it is read from app settings / Key Vault only
/// (app-setting name BraveSearch__ApiKey) and must never be committed to source. Local development
/// uses local.settings.json (git-ignored).
/// </summary>
public sealed class BraveSearchOptions
{
    public const string SectionName = "BraveSearch";

    public string? ApiKey { get; set; }

    /// <summary>Web results fetched per page (Brave caps at 20). More than shown — aggregator
    /// sites and email-less companies are filtered out after the search.</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Country bias for results, defaulting to the UK.</summary>
    public string Country { get; set; } = "GB";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public static BraveSearchOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new BraveSearchOptions
        {
            ApiKey = section["ApiKey"]
        };

        if (int.TryParse(section["PageSize"], out var pageSize) && pageSize is > 0 and <= 20)
            options.PageSize = pageSize;

        var country = section["Country"];
        if (!string.IsNullOrWhiteSpace(country))
            options.Country = country;

        return options;
    }
}
