using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>Sets a drawing's code and title. Either may be blank — see <see cref="RegisterDrawing"/>.</summary>
public sealed record UpdateDrawingMetadata(
    string DrawingId,
    string DrawingCode,
    string Title) : ICommand<Drawing>;
