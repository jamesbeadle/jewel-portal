using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.SiteAccess;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jewel.JPMS.Tests;

/// <summary>One project's register for the site link tests: Structural (with Steel inside it)
/// and Architectural, a drawing with an approved revision and an older one, a drawing with two
/// unapproved revisions, and a second project's drawing that no link on the first may reach.</summary>
internal static class SiteDrawingLinkFixture
{
    public const string ProjectId = "proj-1";
    public const string Structural = "folder-structural";
    public const string Steel = "folder-steel";
    public const string Architectural = "folder-architectural";
    public const string StructuralApproved = "rev-structural-approved";
    public const string StructuralNewestUnapproved = "rev-structural-newest";
    public const string SteelOnly = "rev-steel";
    public const string ArchitecturalApproved = "rev-architectural";
    public const string OtherProjects = "rev-other-project";

    public sealed record SeededLink(SiteDrawingLinkEntity Entity, string Token);

    public static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"site-links-{Guid.NewGuid():N}").Options);

    public static AuditTrail Audit(JpmsContext context) =>
        new(context, new AuditActor { Email = "pm@jewelbb.co.uk" }, NullLogger<AuditTrail>.Instance);

    public static string TokenOf(string url) => url[(url.LastIndexOf('/') + 1)..];

    public static HttpRequest RequestFor(string query)
    {
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        request.QueryString = new QueryString(query);
        return request;
    }

    public static async Task<SeededLink> Link(
        JpmsContext context, string token, string folderId = Structural, bool includeSubFolders = true,
        DateTimeOffset? expiresAt = null, DateTimeOffset? revokedAt = null)
    {
        var entity = new SiteDrawingLinkEntity
        {
            SiteDrawingLinkId = $"link-{token}",
            ProjectId = ProjectId,
            DrawingFolderId = folderId,
            IncludeSubFolders = includeSubFolders,
            Label = token,
            TokenHash = SiteDrawingLinkSecrets.HashOf(token),
            CreatedByEmail = "pm@jewelbb.co.uk",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(30),
            RevokedAt = revokedAt
        };
        context.SiteDrawingLinks.Add(entity);
        await context.SaveChangesAsync();
        return new SeededLink(entity, token);
    }

    public static async Task SeedRegister(JpmsContext context)
    {
        context.Projects.Add(new ProjectEntity { ProjectId = ProjectId, Reference = "JBB-2026-001", Name = "By France" });
        context.Projects.Add(new ProjectEntity { ProjectId = "proj-2", Reference = "JBB-2026-002", Name = "Abbot Road" });
        context.DrawingFolders.AddRange(
            Folder(Structural, "Structural", parentId: null),
            Folder(Steel, "Steel", parentId: Structural),
            Folder(Architectural, "Architectural", parentId: null),
            Folder("folder-other", "Other", parentId: null, projectId: "proj-2"));
        context.Drawings.AddRange(
            Drawing("drawing-s100", "S-100", "Foundations", Structural),
            Drawing("drawing-s200", "S-200", "Beams", Structural),
            Drawing("drawing-st1", "ST-1", "Steel schedule", Steel),
            Drawing("drawing-a1", "A-1", "Ground floor plan", Architectural),
            Drawing("drawing-x1", "X-1", "Elsewhere", "folder-other", projectId: "proj-2"));
        context.DrawingRevisions.AddRange(
            Revision("rev-structural-older", "drawing-s100", "A", DrawingApprovalStatus.Unapproved, daysAgo: 3),
            Revision(StructuralApproved, "drawing-s100", "B", DrawingApprovalStatus.Approved, daysAgo: 10),
            Revision("rev-structural-oldest", "drawing-s200", "A", DrawingApprovalStatus.Unapproved, daysAgo: 20),
            Revision(StructuralNewestUnapproved, "drawing-s200", "B", DrawingApprovalStatus.Unapproved, daysAgo: 5),
            Revision(SteelOnly, "drawing-st1", "A", DrawingApprovalStatus.Unapproved, daysAgo: 1),
            Revision(ArchitecturalApproved, "drawing-a1", "C", DrawingApprovalStatus.Approved, daysAgo: 7),
            Revision(OtherProjects, "drawing-x1", "A", DrawingApprovalStatus.Approved, daysAgo: 7));
        await context.SaveChangesAsync();
    }

    private static DrawingFolderEntity Folder(string id, string name, string? parentId, string projectId = ProjectId) =>
        new() { DrawingFolderId = id, ProjectId = projectId, Name = name, ParentDrawingFolderId = parentId, CreatedAt = DateTimeOffset.UtcNow };

    private static DrawingEntity Drawing(string id, string code, string title, string folderId, string projectId = ProjectId) =>
        new() { DrawingId = id, ProjectId = projectId, DrawingCode = code, Title = title, DrawingFolderId = folderId, CreatedAt = DateTimeOffset.UtcNow };

    private static DrawingRevisionEntity Revision(string id, string drawingId, string label, DrawingApprovalStatus status, int daysAgo) =>
        new()
        {
            DrawingRevisionId = id, DrawingId = drawingId, RevisionLabel = label, FileName = $"{drawingId}-{label}.pdf",
            ReceivedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo), ApprovalStatus = (int)status,
            BlobRef = $"{ProjectId}/{drawingId}/{id}/{drawingId}-{label}.pdf", ContentType = "application/pdf"
        };
}

/// <summary>A blob store that hands back a few bytes for any reference, so the file endpoint can be
/// exercised without Azure.</summary>
internal sealed class StubBlobStore : IDrawingBlobStore
{
    private const string Pdf = "application/pdf";

    public Task<string> UploadAsync(string projectId, string drawingId, string revisionId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        Task.FromResult($"{projectId}/{drawingId}/{revisionId}/{fileName}");

    public Task<DrawingBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<DrawingBlob?>(new DrawingBlob(new MemoryStream(new byte[] { 1, 2, 3 }), Pdf, 3));

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
