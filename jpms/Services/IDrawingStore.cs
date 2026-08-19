using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public interface IDrawingStore
{
    /// <summary>False until the project's drawing register has been fetched at least once.
    /// Lets views distinguish "still loading" from "genuinely not found".</summary>
    bool DrawingsLoadedFor(string projectId);

    IReadOnlyList<Drawing> DrawingsFor(string projectId);

    /// <summary>False until this drawing's revisions have been fetched at least once. Distinct
    /// from <see cref="DrawingsLoadedFor"/>: the register can be here while the revisions are not,
    /// and "no file to preview" is only true once they have landed.</summary>
    bool RevisionsLoadedFor(string drawingId);

    IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId);

    IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId);

    /// <summary>False until the project's drawing folders have been fetched at least once.</summary>
    bool FoldersLoadedFor(string projectId);

    /// <summary>The project's drawing folders, A–Z by name. One flat level — no nesting.</summary>
    IReadOnlyList<DrawingFolder> FoldersFor(string projectId);

    /// <summary>Starts a background refetch of the project's drawings even if cached, and marks
    /// cached revisions stale so the next read refetches them. Call on page entry so navigating
    /// back to the Drawings tab shows fresh data (stale-while-revalidate).</summary>
    void Refresh(string projectId);

    /// <summary>Creates a new named drawing (the "thing") and returns it, optionally filed
    /// straight into a folder (null = ungrouped).</summary>
    Task<Drawing> RegisterDrawingAsync(string projectId, string drawingCode, string title, string? drawingFolderId, CancellationToken cancellationToken);

    /// <summary>Creates a folder on the project's register; creating an existing name
    /// (case-insensitive) returns the existing folder rather than a duplicate.</summary>
    Task<DrawingFolder> CreateFolderAsync(string projectId, string name, CancellationToken cancellationToken);

    /// <summary>Renames a folder; the drawings inside move with it.</summary>
    Task RenameFolderAsync(string projectId, string folderId, string name, CancellationToken cancellationToken);

    /// <summary>Deletes a folder. Its drawings are not deleted — they become ungrouped.</summary>
    Task DeleteFolderAsync(string projectId, string folderId, CancellationToken cancellationToken);

    /// <summary>Moves a drawing into a folder, or out of any folder when the id is null.</summary>
    Task MoveToFolderAsync(string projectId, string drawingId, string? folderId, CancellationToken cancellationToken);

    /// <summary>Uploads a file as a new Unapproved revision of a drawing.</summary>
    Task UploadRevisionAsync(
        string projectId, string drawingId, string revisionLabel, string issuedByEmail,
        IBrowserFile file, CancellationToken cancellationToken);

    /// <summary>Approves a revision — it becomes the latest and all others are archived.</summary>
    Task ApproveRevisionAsync(string projectId, string drawingId, string revisionId, CancellationToken cancellationToken);

    /// <summary>Permanently deletes a drawing, all of its revisions and their stored files.
    /// Administrator, Managing Director and Project Manager only; cannot be undone.</summary>
    Task DeleteDrawingAsync(string projectId, string drawingId, CancellationToken cancellationToken);

    /// <summary>Permanently deletes a single revision and its stored file. Administrator,
    /// Managing Director and Project Manager only; cannot be undone.</summary>
    Task DeleteRevisionAsync(string projectId, string drawingId, string revisionId, CancellationToken cancellationToken);

    event Action? OnChange;
}
