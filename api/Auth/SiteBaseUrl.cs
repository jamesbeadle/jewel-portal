using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// The public site that emailed links (invite, password reset) should point at. Prefers the
/// configured PublicSiteUrl so links survive being served from the raw Function App host, which is
/// not where the user's browser should land.
/// </summary>
public static class SiteBaseUrl
{
    public static string Resolve(IConfiguration configuration, HttpRequest request)
    {
        var configured = configuration["PublicSiteUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
        return $"{request.Scheme}://{request.Host.Value}";
    }
}
