using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IDrawingStore
{
    IReadOnlyList<Drawing> DrawingsFor(string projectId);

    Drawing? Find(string drawingId);

    Drawing Upsert(Drawing drawing);

    IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId);

    DrawingRevision AddRevision(DrawingRevision revision);

    IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId);

    event Action? OnChange;
}
