using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

// Drawing folders: one flat level of named groups on a project's drawing register (no nesting).
// Kept in one file because the whole feature is five small messages over one table.

/// <summary>A project's drawing folders, A–Z by name.</summary>
public sealed record ListDrawingFoldersForProject(string ProjectId)
    : IQuery<IReadOnlyList<DrawingFolder>>;

/// <summary>
/// Creates a folder on the project's drawing register. Creating a name that already exists on the
/// project (case-insensitive) returns the existing folder rather than a duplicate, so the inline
/// "New folder…" path on the upload form cannot split one discipline across two folders.
/// </summary>
public sealed record CreateDrawingFolder(string ProjectId, string Name) : ICommand<DrawingFolder>;

/// <summary>Renames a folder. The drawings inside it move with it — they reference the id.</summary>
public sealed record RenameDrawingFolder(string DrawingFolderId, string Name) : ICommand<DrawingFolder>;

/// <summary>
/// Deletes a folder. The drawings inside it are NOT deleted — they become ungrouped
/// (DrawingFolderId set to null) and drop back into the register's Ungrouped section.
/// </summary>
public sealed record DeleteDrawingFolder(string DrawingFolderId) : ICommand<Acknowledgement>;

/// <summary>Moves a drawing into a folder, or out of any folder when the id is null.</summary>
public sealed record MoveDrawingToFolder(string DrawingId, string? DrawingFolderId) : ICommand<Drawing>;
