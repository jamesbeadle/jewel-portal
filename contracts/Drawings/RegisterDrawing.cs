using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>
/// Registers a new drawing on a project's register. Code and title are both optional — a
/// drawing may be registered from nothing but a file and named later — so the register names
/// it by its latest file until one is given.
/// </summary>
public sealed record RegisterDrawing(
    string ProjectId,
    string DrawingCode,
    string Title,
    // Optional folder to file the new drawing under; null = ungrouped.
    string? DrawingFolderId = null) : ICommand<Drawing>;
