using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Files a pending Document Control item into a project's drawings — the successor to the retired
// ImportDrawingFromMessage. With a DrawingId the file lands as a revision of that drawing;
// otherwise the drawing is matched by code (case-insensitive) within the project, and a new or
// blank code registers a new drawing. Either way the file lands as an Unapproved revision, exactly
// as if uploaded by hand. Code, title and revision are all optional. Bytes come from the
// document-control blob (not the mailbox), so filing works long after the email has moved on;
// IssuedByEmail is the item's snapshotted sender. Returns the item, now Filed.
public sealed record FileDocumentAsDrawing(
    string DocumentControlItemId,
    string ProjectId,
    string DrawingCode,
    string Title,
    string RevisionLabel,
    string? DrawingId = null) : ICommand<DocumentControlItem>;
