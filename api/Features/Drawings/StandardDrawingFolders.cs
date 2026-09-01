using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Drawings;

/// <summary>
/// The folder set every project's drawing register starts with — seeded on both project-creation
/// paths (the manual shell and the tender-enquiry lead) so a new register never opens empty and
/// filing is consistent across projects. Find-or-create by name (case-insensitive, top level),
/// so seeding a project that already has some of the set adds only the missing ones and can
/// never duplicate. The one-off backfill for pre-existing projects is
/// scripts/seed-standard-drawing-folders.sql — keep its name list in step with this one.
/// </summary>
public static class StandardDrawingFolders
{
    public static readonly IReadOnlyList<string> Names = new[]
    {
        "Architect",
        "As Built",
        "Drainage",
        "Finishes",
        "Reports",
        "Specification",
        "Structural",
        "Sub-Contractor"
    };

    /// <summary>
    /// Adds whichever standard folders the project is missing to the context. The caller saves —
    /// on the creation paths the folders ride the same SaveChanges as the project row itself.
    /// </summary>
    public static async Task AddMissingAsync(JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var existing = await context.DrawingFolders.AsNoTracking()
            .Where(folder => folder.ProjectId == projectId && folder.ParentDrawingFolderId == null)
            .Select(folder => folder.Name)
            .ToListAsync(cancellationToken);
        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        foreach (var name in Names)
        {
            if (!taken.Add(name)) continue;
            context.DrawingFolders.Add(new DrawingFolderEntity
            {
                DrawingFolderId = DrawingIdentifierFactory.NextDrawingFolderId(),
                ProjectId = projectId,
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow,
                ParentDrawingFolderId = null
            });
        }
    }
}
