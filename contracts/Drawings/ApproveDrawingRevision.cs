using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>
/// Approves a drawing revision. The target becomes the single Approved (latest) revision of its
/// drawing; every other revision of that drawing is archived, and the drawing's current approved
/// revision label is updated.
/// </summary>
public sealed record ApproveDrawingRevision(
    string DrawingId,
    string DrawingRevisionId,
    string ApprovedByEmail) : ICommand<DrawingRevision>;
