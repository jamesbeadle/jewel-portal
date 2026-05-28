using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Drawings;

public sealed class DrawingsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Drawing>> drawingsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<DrawingRevision>> revisionsByDrawing = new();

    public DrawingsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Drawing> DrawingsCurrent(string projectId) =>
        drawingsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Drawing>();

    public IReadOnlyList<DrawingRevision> RevisionsCurrent(string drawingId) =>
        revisionsByDrawing.TryGetValue(drawingId, out var list) ? list : Array.Empty<DrawingRevision>();

    public async Task RefreshDrawingsAsync(string projectId, CancellationToken cancellationToken)
    {
        drawingsByProject[projectId] = await queries.AskAsync(new ListDrawingsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }

    public async Task RefreshRevisionsAsync(string drawingId, CancellationToken cancellationToken)
    {
        revisionsByDrawing[drawingId] = await queries.AskAsync(new ListRevisionsForDrawing(drawingId), cancellationToken);
        OnChanged?.Invoke();
    }
}
