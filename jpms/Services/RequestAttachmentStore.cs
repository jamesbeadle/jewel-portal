using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

/// <summary>
/// Attachments on a request: drawing revisions linked from the project register, and files
/// uploaded straight onto the request (site photos). Deliberately not cached — the lists are short,
/// they change as people add to them, and a stale attachment list on an RFI is worse than a
/// half-second wait.
/// </summary>
public interface IRequestAttachmentStore
{
    Task<IReadOnlyList<RequestAttachment>> ListAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Links drawing revisions from the project register. Re-linking is a no-op.</summary>
    Task<IReadOnlyList<RequestAttachment>> AttachDrawingsAsync(
        string requestId, IReadOnlyList<string> drawingRevisionIds, CancellationToken cancellationToken = default);

    /// <summary>Uploads one or more files (usually photos) onto the request.</summary>
    Task<IReadOnlyList<RequestAttachment>> UploadFilesAsync(
        string requestId, IReadOnlyList<IBrowserFile> files, string? caption,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequestAttachment>> RemoveAsync(
        string requestId, string attachmentId, CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams an uploaded file. <paramref name="inline"/> renders it in
    /// place (thumbnails and the viewer); otherwise the browser downloads it.</summary>
    string FileUrl(string requestId, string attachmentId, bool inline = false) =>
        $"api/requests/{requestId}/attachments/{attachmentId}/file" + (inline ? "?inline=1" : "");
}

public sealed class HttpRequestAttachmentStore : IRequestAttachmentStore
{
    // Phone photos are a few MB; the browser posts straight to the API, so the practical ceiling is
    // the Functions request size, not a SignalR frame.
    private const long MaxUploadBytes = 64L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpRequestAttachmentStore(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<RequestAttachment>> ListAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListRequestAttachments(requestId), cancellationToken);

    public Task<IReadOnlyList<RequestAttachment>> AttachDrawingsAsync(
        string requestId, IReadOnlyList<string> drawingRevisionIds, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new AttachDrawingsToRequest(requestId, drawingRevisionIds), cancellationToken);

    public async Task<IReadOnlyList<RequestAttachment>> UploadFilesAsync(
        string requestId, IReadOnlyList<IBrowserFile> files, string? caption,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
        }
        if (!string.IsNullOrWhiteSpace(caption)) content.Add(new StringContent(caption), "caption");

        var response = await httpClient.PostAsync($"api/requests/{requestId}/attachments", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface the server's own message (storage misconfigured, file too large) rather than
            // a bare status code — these are the failures a user can actually do something about.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        var updated = await response.Content.ReadFromJsonAsync<List<RequestAttachment>>(cancellationToken: cancellationToken);
        return updated ?? await ListAsync(requestId, cancellationToken);
    }

    public Task<IReadOnlyList<RequestAttachment>> RemoveAsync(
        string requestId, string attachmentId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RemoveRequestAttachment(requestId, attachmentId), cancellationToken);
}
