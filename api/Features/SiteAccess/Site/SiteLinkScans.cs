using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>Counts a scan on its link — the only thing the anonymous surface ever writes about a
/// link — so the register shows a poster is in use.</summary>
public sealed class SiteLinkScans
{
    private readonly JpmsContext context;

    public SiteLinkScans(JpmsContext context) { this.context = context; }

    public async Task RecordAsync(SiteDrawingLinkEntity link, CancellationToken cancellationToken)
    {
        link.ScanCount += 1;
        link.LastScannedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}
