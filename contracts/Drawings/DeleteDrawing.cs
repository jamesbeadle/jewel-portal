using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>
/// Permanently deletes a drawing, all of its revisions, their stored files, and any issue records
/// that referenced those revisions. Restricted to Administrator, Managing Director and Project
/// Manager. This is a hard delete and cannot be undone.
/// </summary>
public sealed record DeleteDrawing(string DrawingId) : ICommand<Acknowledgement>;
