using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

public sealed record RegisterDrawing(
    string ProjectId,
    string DrawingCode,
    string Title,
    string InitialRevisionLabel) : ICommand<Drawing>;
