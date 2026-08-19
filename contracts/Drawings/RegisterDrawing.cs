using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

public sealed record RegisterDrawing(
    string ProjectId,
    string DrawingCode,
    string Title,
    // Optional folder to file the new drawing under; null = ungrouped.
    string? DrawingFolderId = null) : ICommand<Drawing>;
