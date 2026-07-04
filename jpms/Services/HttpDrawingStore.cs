using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public sealed class HttpDrawingStore : IDrawingStore
{
    // Effectively "whatever Azure allows" — the browser posts straight to the API (Blazor WASM),
    // so there is no SignalR frame limit; the practical ceiling is the Functions request size.
    private const long MaxUploadBytes = 2L * 1024 * 1024 * 1024;

    private readonly DrawingsReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpDrawingStore(DrawingsReadModel readModel, IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public bool DrawingsLoadedFor(string projectId) => readModel.DrawingsLoaded(projectId);

    public IReadOnlyList<Drawing> DrawingsFor(string projectId)
    {
        readModel.EnsureDrawings(projectId, CancellationToken.None);
        return readModel.DrawingsCurrent(projectId);
    }

    public IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId)
    {
        readModel.EnsureRevisions(drawingId, CancellationToken.None);
        return readModel.RevisionsCurrent(drawingId);
    }

    // Forces a background reload of the drawing register even when cached, and marks revisions
    // stale so the next RevisionsFor read refetches. Pages call this once on entry (never from
    // render) so tab navigation picks up changes made elsewhere.
    public void Refresh(string projectId)
    {
        readModel.MarkRevisionsStale();
        RefreshInBackground(projectId, null);
    }

    public IReadOnlyList<DrawingRevision> AmbiguousFor(string projectId) =>
        DrawingsFor(projectId)
            .SelectMany(drawing => RevisionsFor(drawing.DrawingId))
            .Where(revision => revision.IsAmbiguous)
            .ToList()
            .AsReadOnly();

    public async Task<Drawing> RegisterDrawingAsync(string projectId, string drawingCode, string title, CancellationToken cancellationToken)
    {
        var drawing = await commands.SendAsync(
            new RegisterDrawing(projectId, drawingCode, title), cancellationToken);
        RefreshInBackground(projectId, null);
        return drawing;
    }

    public async Task UploadRevisionAsync(
        string projectId, string drawingId, string revisionLabel, string issuedByEmail,
        IBrowserFile file, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.Name);
        content.Add(new StringContent(revisionLabel), "revisionLabel");
        if (!string.IsNullOrWhiteSpace(issuedByEmail)) content.Add(new StringContent(issuedByEmail), "issuedByEmail");

        var response = await httpClient.PostAsync($"api/drawings/{drawingId}/revisions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface the server's message (e.g. a storage error) rather than a bare status code.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        // The write has been committed. Refresh caches in the background so a slow or
        // stalled refresh cannot keep the upload UI stuck on "Uploading…".
        RefreshInBackground(projectId, drawingId);
    }

    public async Task ApproveRevisionAsync(string projectId, string drawingId, string revisionId, CancellationToken cancellationToken)
    {
        // The API sets the approver from the signed-in user; the email here is ignored server-side.
        await commands.SendAsync(new ApproveDrawingRevision(drawingId, revisionId, string.Empty), cancellationToken);
        RefreshInBackground(projectId, drawingId);
    }

    public async Task DeleteDrawingAsync(string projectId, string drawingId, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new DeleteDrawing(drawingId), cancellationToken);
        RefreshInBackground(projectId, null);
    }

    public async Task DeleteRevisionAsync(string projectId, string drawingId, string revisionId, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new DeleteDrawingRevision(drawingId, revisionId), cancellationToken);
        RefreshInBackground(projectId, drawingId);
    }

    // Refreshes revisions (optional) then drawings without blocking the caller. Views update
    // via readModel.OnChanged when the refresh lands. Refreshing revisions first marks the
    // drawing as loaded before any OnChanged re-render, so it cannot spawn a duplicate fetch.
    private void RefreshInBackground(string projectId, string? drawingId) =>
        _ = RunRefreshAsync(projectId, drawingId);

    private async Task RunRefreshAsync(string projectId, string? drawingId)
    {
        try
        {
            if (drawingId is not null) await readModel.RefreshRevisionsAsync(drawingId, CancellationToken.None);
            await readModel.RefreshDrawingsAsync(projectId, CancellationToken.None);
        }
        catch { /* OnChanged-driven views recover on the next interaction */ }
    }
}
