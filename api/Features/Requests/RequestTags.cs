using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests;

/// <summary>
/// Builds the project-qualified mailbox tag stem for a request. JPMS workflow tags share one flat
/// category space in the projects@ mailbox, and request references are only unique per project
/// (every project has its own "RFI-012"), so the tag stem must carry a project qualifier or two
/// projects' mail would cross-link. Mirrors the cost-centre provider's pattern:
///   stem = "{projectRef}-{reference}"  ->  category "JPMS/JBB-2026-001-RFI-012".
/// The entity's own <see cref="RequestEntity.TagReference"/> remains the unqualified stem (display
/// fallback and pre-qualification legacy tag).
/// </summary>
internal static class RequestTags
{
    /// <summary>The qualified stem from already-loaded parts. Falls back to the (unique) project id
    /// when the project has no human reference yet, so the stem is always project-unique.</summary>
    public static string Stem(string? projectRef, string projectId, string tagReference) =>
        $"{(string.IsNullOrWhiteSpace(projectRef) ? projectId : projectRef.Trim())}-{tagReference}";

    /// <summary>The qualified stem for a request, resolving its project's reference.</summary>
    public static async Task<string> StemAsync(JpmsContext context, RequestEntity entity, CancellationToken cancellationToken)
    {
        var projectRef = await ProjectRefAsync(context, entity.ProjectId, cancellationToken);
        return Stem(projectRef, entity.ProjectId, entity.TagReference);
    }

    /// <summary>The project's human reference (e.g. "JBB-2026-001"), or null when unset.</summary>
    public static async Task<string?> ProjectRefAsync(JpmsContext context, string projectId, CancellationToken cancellationToken) =>
        await context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync(cancellationToken);
}
