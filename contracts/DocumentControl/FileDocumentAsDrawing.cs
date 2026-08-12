using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Files a pending Document Control item into a project's drawings — the successor to the retired
// ImportDrawingFromMessage. The drawing is matched by code (case-insensitive) within the project —
// a new code registers a new drawing — and the file lands as an Unapproved revision, exactly as if
// uploaded by hand. Bytes come from the document-control blob (not the mailbox), so filing works
// long after the email has moved on; IssuedByEmail is the item's snapshotted sender. Returns the
// item, now Filed.
public sealed record FileDocumentAsDrawing(
    string DocumentControlItemId,
    string ProjectId,
    string DrawingCode,
    string Title,
    string RevisionLabel) : ICommand<DocumentControlItem>;
