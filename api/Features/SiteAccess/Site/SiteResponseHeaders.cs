using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>What every anonymous site response says about itself: never cached (a shared site
/// phone must not keep drawings), never indexed, never a referrer (the token would travel in it),
/// never sniffed.</summary>
public static class SiteResponseHeaders
{
    private const string CacheControl = "Cache-Control";
    private const string NeverStore = "no-store";
    private const string Robots = "X-Robots-Tag";
    private const string NeverIndex = "noindex, nofollow";
    private const string ReferrerPolicy = "Referrer-Policy";
    private const string NoReferrer = "no-referrer";

    public static void Apply(HttpResponse response)
    {
        response.Headers[CacheControl] = NeverStore;
        response.Headers[Robots] = NeverIndex;
        response.Headers[ReferrerPolicy] = NoReferrer;
        InlineRendering.ForbidSniffing(response);
    }
}
