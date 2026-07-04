using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>
/// Permanently deletes a single revision of a drawing, its stored file, and any issue records that
/// referenced it. If the deleted revision was the approved one, the drawing reverts to having no
/// approved revision. Restricted to Administrator, Managing Director and Project Manager. This is a
/// hard delete and cannot be undone.
/// </summary>
public sealed record DeleteDrawingRevision(
    string DrawingId,
    string DrawingRevisionId) : ICommand<Acknowledgement>;
