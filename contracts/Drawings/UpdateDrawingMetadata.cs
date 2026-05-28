using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

public sealed record UpdateDrawingMetadata(
    string DrawingId,
    string DrawingCode,
    string Title) : ICommand<Drawing>;
