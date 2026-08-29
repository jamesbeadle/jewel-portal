using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public interface IDrawingStore
{
    /// <summary>False until the project's drawing register has been fetched at least once.
    /// Lets views distinguish "still loading" from "genuinely not found".</summary>
    bool DrawingsLoadedFor(string projectId);

    /// <summary>True when the register's last fetch FAILED. Pair with
    /// <see cref="DrawingsLoadedFor"/> when gating: a failure must open the gate with a message
    /// and a retry, never leave it pulsing (the loading-states rules in CLAUDE.md).</summary>
    bool DrawingsFailedFor(string projectId);

    /// <summary>Clears the register's failed state and fetches it again.</summary>
    void RetryDrawings(string projectId);

    IReadOnlyList<Drawing> DrawingsFor(string projectId);

    /// <summary>False until this drawing's revisions have been fetched at least once. Distinct
    /// from <see cref="DrawingsLoadedFor"/>: the register can be here while the revisions are not,
    /// and "no file to preview" is only true once they have landed.</summary>
    bool RevisionsLoadedFor(string drawingId);

    /// <summary>True when this drawing's last revisions fetch FAILED — same pairing rule as
    /// <see cref="DrawingsFailedFor"/>.</summary>
    bool RevisionsFailedFor(string drawingId);

    /// <summary>Clears the drawing's failed state and fetches its revisions again.</summary>
    void RetryRevisions(string drawingId);

    IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId);

    /// <summary>Fetches a drawing's revisions if they have never loaded, completing once they
    /// have landed — for callers about to act on the list rather than render it (the composer
    /// attaching a ticked drawing's file). Already-loaded revisions return immediately; a failed
    /// fetch is swallowed (the query pipeline has reported it) and
    /// <see cref="RevisionsLoadedFor"/> stays false so the caller can tell.</summary>
    Task EnsureRevisionsNowAsync(string drawingId, CancellationToken cancellationToken);

    IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId);

    /// <summary>False until the project's drawing folders have been fetched at least once.</summary>
    bool FoldersLoadedFor(string projectId);

    /// <summary>The project's drawing folders, every level, A–Z by name. See <see cref="Features.Drawings.DrawingFolderTree"/> for nesting.</summary>
    IReadOnlyList<DrawingFolder> FoldersFor(string projectId);

    /// <summary>Sets a drawing's code and title; either may be blank.</summary>
    Task UpdateDetailsAsync(string projectId, string drawingId, string drawingCode, string title, CancellationToken cancellationToken);

    /// <summary>Sets or clears a revision's label after upload.</summary>
    Task SetRevisionLabelAsync(string projectId, string drawingId, string revisionId, string revisionLabel, CancellationToken cancellationToken);

    /// <summary>Starts a background refetch of the project's drawings even if cached, and marks
    /// cached revisions stale so the next read refetches them. Call on page entry so navigating
    /// back to the Drawings tab shows fresh data (stale-while-revalidate).</summary>
    void Refresh(string projectId);

    /// <summary>Refetches the register, its folders and (when given) one drawing's revisions,
    /// completing only once the reloads have LANDED — so a caller that has just written can keep
    /// its busy state up until the result is actually on screen, instead of closing against stale
    /// data. A failed reload is swallowed (the query pipeline has already reported it) and the
    /// caches catch up through the background refreshes instead.</summary>
    Task RefreshNowAsync(string projectId, string? drawingId, CancellationToken cancellationToken);

    /// <summary>Creates a new named drawing (the "thing") and returns it, optionally filed
    /// straight into a folder (null = ungrouped).</summary>
    Task<Drawing> RegisterDrawingAsync(string projectId, string drawingCode, string title, string? drawingFolderId, CancellationToken cancellationToken);

    /// <summary>Creates a folder on the project's register, top level or inside a parent;
    /// creating a name that already exists at that level (case-insensitive) returns the
    /// existing folder rather than a duplicate.</summary>
    Task<DrawingFolder> CreateFolderAsync(string projectId, string name, string? parentFolderId, CancellationToken cancellationToken);

    /// <summary>Renames a folder; the drawings inside move with it.</summary>
    Task RenameFolderAsync(string projectId, string folderId, string name, CancellationToken cancellationToken);

    /// <summary>Deletes a folder. Its drawings and sub-folders are not deleted — they move up a level.</summary>
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
