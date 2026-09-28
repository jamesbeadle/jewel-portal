using Jewel.JPMS.Contracts.SiteAccess;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.SiteAccess;

public interface ISiteDrawingLinkStore
{
    /// <summary>A project's site links, newest first, active and spent alike.</summary>
    Task<IReadOnlyList<SiteDrawingLink>> ListAsync(string projectId, CancellationToken cancellationToken);

    /// <summary>Mints a link; the answer is the one copy of its URL, QR code and poster.</summary>
    Task<SiteDrawingLinkCreated> CreateAsync(CreateSiteDrawingLink command, CancellationToken cancellationToken);

    /// <summary>Stops a link at once; the same poster answers a plain 404 from the next scan.</summary>
    Task RevokeAsync(string siteDrawingLinkId, CancellationToken cancellationToken);
}
