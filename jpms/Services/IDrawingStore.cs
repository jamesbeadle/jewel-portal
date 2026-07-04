using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public interface IDrawingStore
{
    /// <summary>False until the project's drawing register has been fetched at least once.
    /// Lets views distinguish "still loading" from "genuinely not found".</summary>
    bool DrawingsLoadedFor(string projectId);

    IReadOnlyList<Drawing> DrawingsFor(string projectId);

    IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId);

    IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId);

    /// <summary>Starts a background refetch of the project's drawings even if cached, and marks
    /// cached revisions stale so the next read refetches them. Call on page entry so navigating
    /// back to the Drawings tab shows fresh data (stale-while-revalidate).</summary>
    void Refresh(string projectId);

    /// <summary>Creates a new named drawing (the "thing") and returns it.</summary>
    Task<Drawing> RegisterDrawingAsync(string projectId, string drawingCode, string title, CancellationToken cancellationToken);

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
