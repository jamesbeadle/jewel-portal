using Jewel.JPMS.Api.Data.Entities;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Procurement.Acceptance;

/// <summary>
/// The absolute acceptance link a purchase-order email carries: the configured PublicSiteUrl
/// (the portal, never the raw Function App host — SiteBaseUrl's rule, without a request to fall
/// back on because the automatic send has none) and the page's own path from
/// <see cref="WorkOrderAcceptanceLink"/>. Issuing it mints the order's token on the first send
/// and saves, so the fresh send and the reply-into-a-thread door hand out the same link.
/// </summary>
public sealed class WorkOrderAcceptanceLinks
{
    private const string DefaultPublicSiteUrl = "https://portal.jewelbb.co.uk";

    private readonly string publicSiteUrl;

    public WorkOrderAcceptanceLinks(IConfiguration configuration)
    {
        var configured = configuration["PublicSiteUrl"];
        publicSiteUrl = string.IsNullOrWhiteSpace(configured) ? DefaultPublicSiteUrl : configured;
    }

    public string For(string token) => WorkOrderAcceptanceLink.On(publicSiteUrl, token);

    public async Task<string> IssuedForAsync(JpmsContext context, WorkOrderEntity order, CancellationToken cancellationToken)
    {
        var token = WorkOrderAcceptanceTokens.MintIfMissing(order, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        return For(token);
    }
}
