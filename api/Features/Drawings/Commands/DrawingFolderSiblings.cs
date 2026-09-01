using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// Folder names are unique among siblings — the folders sharing one parent on one project —
/// not across the whole project, so "Planning" can exist under both Architect and Structural.
/// Shared by <see cref="CreateDrawingFolder"/> (find-or-create) and <see cref="RenameDrawingFolder"/>.
/// </summary>
internal static class DrawingFolderSiblings
{
    public static Task<DrawingFolderEntity?> FindByNameAsync(
        JpmsContext context, string projectId, string? parentId, string name, CancellationToken cancellationToken)
    {
        var lowered = name.ToLower();
        return context.DrawingFolders
            .FirstOrDefaultAsync(folder => folder.ProjectId == projectId
                && folder.ParentDrawingFolderId == parentId
                && folder.Name.ToLower() == lowered, cancellationToken);
    }
}
