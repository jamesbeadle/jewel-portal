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

    public IReadOnlyList<Drawing> DrawingsFor(string projectId)
    {
        if (readModel.DrawingsCurrent(projectId).Count == 0) _ = readModel.RefreshDrawingsAsync(projectId, CancellationToken.None);
        return readModel.DrawingsCurrent(projectId);
    }

    public Drawing? Find(string drawingId) =>
        queries.AskAsync(new GetDrawingById(drawingId), CancellationToken.None).GetAwaiter().GetResult();

    public IReadOnlyList<DrawingRevision> RevisionsFor(string drawingId)
    {
        if (readModel.RevisionsCurrent(drawingId).Count == 0) _ = readModel.RefreshRevisionsAsync(drawingId, CancellationToken.None);
        return readModel.RevisionsCurrent(drawingId);
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
        await readModel.RefreshDrawingsAsync(projectId, cancellationToken);
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
        response.EnsureSuccessStatusCode();

        await readModel.RefreshRevisionsAsync(drawingId, cancellationToken);
        await readModel.RefreshDrawingsAsync(projectId, cancellationToken);
    }

    public async Task ApproveRevisionAsync(string projectId, string drawingId, string revisionId, CancellationToken cancellationToken)
    {
        // The API sets the approver from the signed-in user; the email here is ignored server-side.
        await commands.SendAsync(new ApproveDrawingRevision(drawingId, revisionId, string.Empty), cancellationToken);
        await readModel.RefreshRevisionsAsync(drawingId, cancellationToken);
        await readModel.RefreshDrawingsAsync(projectId, cancellationToken);
    }
}
