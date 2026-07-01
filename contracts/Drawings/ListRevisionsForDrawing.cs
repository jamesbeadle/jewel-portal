using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

public sealed record ListRevisionsForDrawing(
    string DrawingId,
    DrawingRevisionStatusFilter Status = DrawingRevisionStatusFilter.All) : IQuery<IReadOnlyList<DrawingRevision>>;
