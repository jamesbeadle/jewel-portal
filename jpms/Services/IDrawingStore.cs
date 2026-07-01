using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public interface IDrawingStore
{
    IReadOnlyList<Drawing> DrawingsFor(string projectId);

    IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId);

    IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId);

    /// <summary>Creates a new named drawing (the "thing") and returns it.</summary>
    Task<Drawing> RegisterDrawingAsync(string projectId, string drawingCode, string title, CancellationToken cancellationToken);

    /// <summary>Uploads a file as a new Unapproved revision of a drawing.</summary>
    Task UploadRevisionAsync(
        string projectId, string drawingId, string revisionLabel, string issuedByEmail,
        IBrowserFile file, CancellationToken cancellationToken);

    /// <summary>Approves a revision — it becomes the latest and all others are archived.</summary>
    Task ApproveRevisionAsync(string projectId, string drawingId, string revisionId, CancellationToken cancellationToken);

    event Action? OnChange;
}
