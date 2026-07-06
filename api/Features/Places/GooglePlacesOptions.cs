using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Configuration for outbound calls to the Google Places API (New) — used to find local
/// subcontractors near a project's site. The API key is a secret: it is read from app settings /
/// Key Vault only (app-setting name GooglePlaces__ApiKey) and must never be committed to source.
/// Local development uses local.settings.json (git-ignored).
/// </summary>
public sealed class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    public string? ApiKey { get; set; }

    /// <summary>Results per page (Places caps text search at 20).</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Region bias for results, defaulting to the UK.</summary>
    public string RegionCode { get; set; } = "GB";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public static GooglePlacesOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new GooglePlacesOptions
        {
            ApiKey = section["ApiKey"]
        };

        if (int.TryParse(section["PageSize"], out var pageSize) && pageSize is > 0 and <= 20)
            options.PageSize = pageSize;

        var region = section["RegionCode"];
        if (!string.IsNullOrWhiteSpace(region))
            options.RegionCode = region;

        return options;
    }
}
