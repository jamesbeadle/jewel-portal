using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.SiteAccess.Documents;
using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

/// <summary>
/// Mints the link: a fresh secret, its hash stored with the row, and then — while the secret is
/// still in hand, because nothing stored can reproduce them — the URL, the QR code and the poster.
/// </summary>
public sealed class CreateSiteDrawingLinkHandler : ICommandHandler<CreateSiteDrawingLink, SiteDrawingLinkCreated>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public CreateSiteDrawingLinkHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<SiteDrawingLinkCreated> HandleAsync(CreateSiteDrawingLink command, CancellationToken cancellationToken)
    {
        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProjectId == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");
        var folder = await context.DrawingFolders.AsNoTracking()
            .FirstOrDefaultAsync(row => row.DrawingFolderId == command.DrawingFolderId, cancellationToken);
        var isFolderOnProject = folder is not null && folder.ProjectId == command.ProjectId;
        if (!isFolderOnProject) throw new InvalidOperationException("That folder is not on this project's register.");

        var token = SiteDrawingLinkSecrets.NewToken();
        var link = NewLink(command, token);
        context.SiteDrawingLinks.Add(link);
        await context.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            AuditEventType.SiteDrawingLinkCreated,
            $"Site link minted for “{folder!.Name}” — {link.Label}, expires {link.ExpiresAt:dd MMM yyyy}.",
            projectId: link.ProjectId,
            recordReference: link.Label,
            actorEmail: command.CreatedByEmail,
            cancellationToken: cancellationToken);

        var url = SiteDrawingLinkSecrets.UrlFor(command.SiteOrigin, token);
        var qrPng = SiteDrawingLinkQrCode.Png(url);
        var poster = new SiteDrawingLinkPoster(project.Name, project.Reference, link.Label, link.CreatedAt, link.ExpiresAt, qrPng);
        var posterPdf = SiteDrawingLinkPosterRenderer.Render(poster);
        return new SiteDrawingLinkCreated(link.ToModel(), url, Convert.ToBase64String(qrPng), Convert.ToBase64String(posterPdf));
    }

    private static SiteDrawingLinkEntity NewLink(CreateSiteDrawingLink command, string token)
    {
        var now = DateTimeOffset.UtcNow;
        return new SiteDrawingLinkEntity
        {
            SiteDrawingLinkId = Guid.NewGuid().ToString("N"),
            ProjectId = command.ProjectId,
            DrawingFolderId = command.DrawingFolderId,
            IncludeSubFolders = command.IncludeSubFolders,
            Label = command.Label.Trim(),
            TokenHash = SiteDrawingLinkSecrets.HashOf(token),
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = now,
            ExpiresAt = now.AddDays(command.ExpiresInDays)
        };
    }
}
