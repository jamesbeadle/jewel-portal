using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>Each folder's path as the register shows it — "Architect / Planning" — keyed by folder
/// id, so a scan's page can head each group with where its drawings live.</summary>
public static class SiteFolderPaths
{
    private const string Separator = " / ";

    public static IReadOnlyDictionary<string, string> Of(IReadOnlyList<DrawingFolderEntity> folders)
    {
        var byId = folders.ToDictionary(folder => folder.DrawingFolderId);
        return folders.ToDictionary(folder => folder.DrawingFolderId, folder => PathOf(folder, byId));
    }

    private static string PathOf(DrawingFolderEntity folder, IReadOnlyDictionary<string, DrawingFolderEntity> byId)
    {
        var names = new List<string>();
        var visited = new HashSet<string>();
        var current = folder;
        while (current is not null && visited.Add(current.DrawingFolderId))
        {
            names.Insert(0, current.Name);
            current = ParentOf(current, byId);
        }
        return string.Join(Separator, names);
    }

    private static DrawingFolderEntity? ParentOf(DrawingFolderEntity folder, IReadOnlyDictionary<string, DrawingFolderEntity> byId)
    {
        if (folder.ParentDrawingFolderId is not { } parentId) return null;
        return byId.TryGetValue(parentId, out var parent) ? parent : null;
    }
}
