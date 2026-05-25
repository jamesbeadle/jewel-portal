using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryDrawingStore : IDrawingStore
{
    private readonly List<Drawing> drawings = new()
    {
        new("DR-001", "PRJ-001", "A-100", "Ground floor plan",         "C", DateTimeOffset.UtcNow.AddDays(-90)),
        new("DR-002", "PRJ-001", "A-101", "First floor plan",          "B", DateTimeOffset.UtcNow.AddDays(-85)),
        new("DR-003", "PRJ-001", "A-200", "South elevation",           "A", DateTimeOffset.UtcNow.AddDays(-60)),
        new("DR-004", "PRJ-001", "S-101", "Foundation reinforcement",  "B", DateTimeOffset.UtcNow.AddDays(-45))
    };

    private readonly List<DrawingRevision> revisions = new()
    {
        new("RV-001", "DR-001", "A", "A-100_Rev-A.pdf", "architect@example.com", DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow.AddDays(-60), false, 3),
        new("RV-002", "DR-001", "B", "A-100_Rev-B.pdf", "architect@example.com", DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow.AddDays(-30), false, 5),
        new("RV-003", "DR-001", "C", "A-100_Rev-C.pdf", "architect@example.com", DateTimeOffset.UtcNow.AddDays(-30), null, false, 8)
    };

    public event Action? OnChange;

    public IReadOnlyList<Drawing> DrawingsFor(string projectId) =>
        drawings.Where(drawing =>
            string.Equals(drawing.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(drawing => drawing.DrawingCode)
                .ToList()
                .AsReadOnly();

    public Drawing? Find(string drawingId) =>
        drawings.FirstOrDefault(drawing =>
            string.Equals(drawing.DrawingId, drawingId, StringComparison.OrdinalIgnoreCase));

    public Drawing Upsert(Drawing drawing)
    {
        var existing = Find(drawing.DrawingId);
        if (existing is not null) drawings.Remove(existing);
        drawings.Add(drawing);
        OnChange?.Invoke();
        return drawing;
    }

    public IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId) =>
        revisions.Where(revision =>
            string.Equals(revision.DrawingId, drawingId, StringComparison.OrdinalIgnoreCase))
                 .OrderByDescending(revision => revision.ReceivedAt)
                 .ToList()
                 .AsReadOnly();

    public DrawingRevision AddRevision(DrawingRevision revision)
    {
        SupersedePreviousRevisions(revision.DrawingId);
        revisions.Add(revision);
        UpdateDrawingCurrentRevision(revision.DrawingId, revision.RevisionLabel);
        OnChange?.Invoke();
        return revision;
    }

    public IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId)
    {
        var projectDrawings = DrawingsFor(projectId).Select(drawing => drawing.DrawingId).ToHashSet();
        return revisions
            .Where(revision => revision.IsAmbiguous && projectDrawings.Contains(revision.DrawingId))
            .ToList()
            .AsReadOnly();
    }

    private void SupersedePreviousRevisions(string drawingId)
    {
        for (var i = 0; i < revisions.Count; i++)
        {
            if (revisions[i].DrawingId == drawingId && revisions[i].SupersededAt is null)
            {
                revisions[i] = revisions[i] with { SupersededAt = DateTimeOffset.UtcNow };
            }
        }
    }

    private void UpdateDrawingCurrentRevision(string drawingId, string newRevision)
    {
        var drawing = Find(drawingId);
        if (drawing is null) return;
        Upsert(drawing with { CurrentRevision = newRevision });
    }
}
