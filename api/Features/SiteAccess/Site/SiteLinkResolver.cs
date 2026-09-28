using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>
/// The one resolution behind both anonymous site endpoints: hash what arrived, look it up, and
/// answer nothing — the same nothing — for a token that is unknown, revoked or expired, so a
/// prober learns nothing from the difference. A refusal is a plain 404 with no body, logged with
/// the caller's address and never the token. Nothing is cached, so a revoke bites on the next scan.
/// </summary>
public sealed class SiteLinkResolver
{
    private readonly JpmsContext context;
    private readonly ILogger<SiteLinkResolver> logger;

    public SiteLinkResolver(JpmsContext context, ILogger<SiteLinkResolver> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task<SiteDrawingLinkEntity?> ResolveAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var tokenHash = SiteDrawingLinkSecrets.HashOf(token);
        var link = await context.SiteDrawingLinks
            .FirstOrDefaultAsync(row => row.TokenHash == tokenHash, cancellationToken);
        if (link is null) return null;
        var isSpent = link.RevokedAt is not null || link.ExpiresAt <= DateTimeOffset.UtcNow;
        return isSpent ? null : link;
    }

    public IActionResult Refuse(HttpRequest request)
    {
        logger.LogInformation("Site link refused for {Address}", ClientKey.Of(request));
        return new NotFoundResult();
    }
}
