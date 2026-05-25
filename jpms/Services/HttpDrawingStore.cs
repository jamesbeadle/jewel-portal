using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpDrawingStore : IDrawingStore
{
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, IReadOnlyList<Drawing>> drawingsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<DrawingRevision>> revisionsByDrawing = new();

    public HttpDrawingStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public event Action? OnChange;

    public IReadOnlyList<Drawing> DrawingsFor(string projectId)
    {
        if (!drawingsByProject.ContainsKey(projectId)) _ = LoadDrawingsAsync(projectId);
        return drawingsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Drawing>();
    }

    public Drawing? Find(string drawingId) =>
        drawingsByProject.Values.SelectMany(list => list)
            .FirstOrDefault(d => string.Equals(d.DrawingId, drawingId, StringComparison.OrdinalIgnoreCase));

    public Drawing Upsert(Drawing drawing)
    {
        _ = PostAsync("/api/drawings", drawing, drawing.ProjectId);
        return drawing;
    }

    public IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId)
    {
        if (!revisionsByDrawing.ContainsKey(drawingId)) _ = LoadRevisionsAsync(drawingId);
        return revisionsByDrawing.TryGetValue(drawingId, out var list) ? list : Array.Empty<DrawingRevision>();
    }

    public DrawingRevision AddRevision(DrawingRevision revision)
    {
        _ = PostAndRefreshRevisionsAsync(revision);
        return revision;
    }

    public IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId) =>
        DrawingsFor(projectId)
            .SelectMany(drawing => RevisionsFor(drawing.DrawingId))
            .Where(revision => revision.IsAmbiguous)
            .ToList()
            .AsReadOnly();

    private async Task LoadDrawingsAsync(string projectId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Drawing>>($"/api/projects/{projectId}/drawings");
            drawingsByProject[projectId] = response?.AsReadOnly() ?? (IReadOnlyList<Drawing>)Array.Empty<Drawing>();
            OnChange?.Invoke();
        }
        catch { drawingsByProject[projectId] = Array.Empty<Drawing>(); }
    }

    private async Task LoadRevisionsAsync(string drawingId)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<DrawingRevision>>($"/api/drawings/{drawingId}/revisions");
            revisionsByDrawing[drawingId] = response?.AsReadOnly() ?? (IReadOnlyList<DrawingRevision>)Array.Empty<DrawingRevision>();
            OnChange?.Invoke();
        }
        catch { revisionsByDrawing[drawingId] = Array.Empty<DrawingRevision>(); }
    }

    private async Task PostAsync<T>(string url, T body, string projectId)
    {
        try { await httpClient.PostAsJsonAsync(url, body); } catch { return; }
        drawingsByProject.Remove(projectId);
        await LoadDrawingsAsync(projectId);
    }

    private async Task PostAndRefreshRevisionsAsync(DrawingRevision revision)
    {
        try { await httpClient.PostAsJsonAsync("/api/drawings/revisions", revision); } catch { return; }
        revisionsByDrawing.Remove(revision.DrawingId);
        await LoadRevisionsAsync(revision.DrawingId);
    }
}
