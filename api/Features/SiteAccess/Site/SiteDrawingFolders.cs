using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>The folders a link reaches: its own, and every folder beneath it when the link
/// includes sub-folders — walked over the project's folder rows, which are few.</summary>
public static class SiteDrawingFolders
{
    public static IReadOnlyList<string> Reached(SiteDrawingLinkEntity link, IReadOnlyList<DrawingFolderEntity> projectFolders)
    {
        var reached = new List<string> { link.DrawingFolderId };
        if (!link.IncludeSubFolders) return reached;

        var childrenByParent = projectFolders.ToLookup(folder => folder.ParentDrawingFolderId);
        var frontier = new Queue<string>();
        frontier.Enqueue(link.DrawingFolderId);
        while (frontier.TryDequeue(out var parentId))
        {
            foreach (var child in childrenByParent[parentId])
            {
                if (reached.Contains(child.DrawingFolderId)) continue;
                reached.Add(child.DrawingFolderId);
                frontier.Enqueue(child.DrawingFolderId);
            }
        }
        return reached;
    }
}
