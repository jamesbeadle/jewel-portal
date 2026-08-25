using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>
/// Sets (or clears) a revision's label after upload — revisions may be uploaded without one.
/// When the revision is the drawing's Approved one, the drawing's current approved label follows.
/// </summary>
public sealed record SetDrawingRevisionLabel(
    string DrawingId,
    string DrawingRevisionId,
    string RevisionLabel) : ICommand<DrawingRevision>;
