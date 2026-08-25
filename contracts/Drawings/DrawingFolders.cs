using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

// Drawing folders: named groups on a project's drawing register. Folders nest — a folder created
// with a parent is a sub-folder — and drawings may sit at any level. Kept in one file because the
// whole feature is a handful of small messages over one table.

/// <summary>A project's drawing folders, every level, A–Z by name. The tree is built client-side
/// from <see cref="DrawingFolder.ParentDrawingFolderId"/>.</summary>
public sealed record ListDrawingFoldersForProject(string ProjectId)
    : IQuery<IReadOnlyList<DrawingFolder>>;

/// <summary>
/// Creates a folder on the project's drawing register, at the top level or inside
/// <see cref="ParentDrawingFolderId"/>. Creating a name that already exists at that level
/// (case-insensitive) returns the existing folder rather than a duplicate, so the inline
/// "New folder…" path on the upload form cannot split one discipline across two folders.
/// </summary>
public sealed record CreateDrawingFolder(
    string ProjectId,
    string Name,
    string? ParentDrawingFolderId = null) : ICommand<DrawingFolder>;

/// <summary>Renames a folder. The drawings and sub-folders inside it move with it — they reference the id.</summary>
public sealed record RenameDrawingFolder(string DrawingFolderId, string Name) : ICommand<DrawingFolder>;

/// <summary>
/// Deletes a folder. Nothing inside it is deleted — its drawings and sub-folders move up one
/// level into the deleted folder's parent (or, for a top-level folder, back to the register's
/// Ungrouped section and top level).
/// </summary>
public sealed record DeleteDrawingFolder(string DrawingFolderId) : ICommand<Acknowledgement>;

/// <summary>Moves a drawing into a folder (any level), or out of any folder when the id is null.</summary>
public sealed record MoveDrawingToFolder(string DrawingId, string? DrawingFolderId) : ICommand<Drawing>;
