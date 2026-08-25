using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Drawings;

/// <summary>
/// The folder tree behind a project's drawing register, built from the flat folder list the API
/// returns. Nodes come out in display order — depth-first, siblings A–Z as supplied — each
/// knowing its depth and its path ("Architect / Planning"), which is all the register table and
/// the folder pickers need to draw nesting.
/// </summary>
public sealed class DrawingFolderTree
{
    public sealed record Node(DrawingFolder Folder, int Depth, string Path);

    private const string PathSeparator = " / ";

    private readonly IReadOnlyList<Node> nodes;
    private readonly Dictionary<string, Node> byId;
    private readonly ILookup<string?, DrawingFolder> childrenByParent;

    public DrawingFolderTree(IReadOnlyList<DrawingFolder> folders)
    {
        childrenByParent = folders.ToLookup(folder => ParentOrNull(folder, folders));
        var ordered = new List<Node>();
        AppendChildren(parentId: null, depth: 0, path: "", ordered);
        nodes = ordered;
        byId = ordered.ToDictionary(node => node.Folder.DrawingFolderId);
    }

    /// <summary>Every folder in display order, top level first and each sub-tree beneath its parent.</summary>
    public IReadOnlyList<Node> Nodes => nodes;

    public IEnumerable<DrawingFolder> ChildrenOf(string? parentId) => childrenByParent[parentId];

    public string PathOf(string? folderId) =>
        folderId is not null && byId.TryGetValue(folderId, out var node) ? node.Path : "";

    public int DepthOf(string? folderId) =>
        folderId is not null && byId.TryGetValue(folderId, out var node) ? node.Depth : 0;

    /// <summary>The folder and every folder beneath it — what collapsing or counting a folder covers.</summary>
    public IEnumerable<string> SubtreeIds(string folderId)
    {
        yield return folderId;
        foreach (var child in ChildrenOf(folderId))
            foreach (var descendant in SubtreeIds(child.DrawingFolderId))
                yield return descendant;
    }

    public bool HasAncestor(string? folderId, IReadOnlySet<string> candidates)
    {
        var current = folderId is not null && byId.TryGetValue(folderId, out var node) ? node.Folder : null;
        while (current?.ParentDrawingFolderId is { } parentId)
        {
            if (candidates.Contains(parentId)) return true;
            current = byId.TryGetValue(parentId, out var parent) ? parent.Folder : null;
        }
        return false;
    }

    private void AppendChildren(string? parentId, int depth, string path, List<Node> ordered)
    {
        foreach (var folder in childrenByParent[parentId])
        {
            var folderPath = path.Length == 0 ? folder.Name : path + PathSeparator + folder.Name;
            ordered.Add(new Node(folder, depth, folderPath));
            AppendChildren(folder.DrawingFolderId, depth + 1, folderPath, ordered);
        }
    }

    // A parent that is not in the list (deleted out from under us mid-refresh) is treated as
    // top level rather than making the folder vanish from the register.
    private static string? ParentOrNull(DrawingFolder folder, IReadOnlyList<DrawingFolder> all) =>
        all.Any(candidate => candidate.DrawingFolderId == folder.ParentDrawingFolderId)
            ? folder.ParentDrawingFolderId
            : null;
}
