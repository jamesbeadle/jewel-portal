using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

/// <summary>Stamps the link revoked. Nothing is cached anywhere, so the next scan of the same
/// poster is refused; revoking twice is the same one act.</summary>
public sealed class RevokeSiteDrawingLinkHandler : ICommandHandler<RevokeSiteDrawingLink, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public RevokeSiteDrawingLinkHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<Acknowledgement> HandleAsync(RevokeSiteDrawingLink command, CancellationToken cancellationToken)
    {
        var link = await context.SiteDrawingLinks
            .FirstOrDefaultAsync(row => row.SiteDrawingLinkId == command.SiteDrawingLinkId, cancellationToken)
            ?? throw new InvalidOperationException("Site link not found.");
        if (link.RevokedAt is not null) return new Acknowledgement(link.SiteDrawingLinkId);

        link.RevokedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            AuditEventType.SiteDrawingLinkRevoked,
            $"Site link revoked — {link.Label}, after {link.ScanCount} scans.",
            projectId: link.ProjectId,
            recordReference: link.Label,
            actorEmail: command.RevokedByEmail,
            cancellationToken: cancellationToken);
        return new Acknowledgement(link.SiteDrawingLinkId);
    }
}
