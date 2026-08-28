using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

/// <summary>
/// Attachments kept on a work order for record keeping — the quote the order was raised against,
/// a signed copy, a photo of the scope. Never sent to the supplier: the purchase-order email and
/// printed PO ignore them. Deliberately not cached — the lists are short and change as people
/// add to them, same reasoning as the request attachment store.
/// </summary>
public interface IWorkOrderAttachmentStore
{
    Task<IReadOnlyList<WorkOrderAttachment>> ListAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>Uploads one or more files from this computer onto the order.</summary>
    Task<IReadOnlyList<WorkOrderAttachment>> UploadFilesAsync(
        string workOrderId, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkOrderAttachment>> RemoveAsync(
        string workOrderId, string attachmentId, CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams a stored file. <paramref name="inline"/> renders it in
    /// place (thumbnails and previews); otherwise the browser downloads it.</summary>
    string FileUrl(string workOrderId, string attachmentId, bool inline = false) =>
        $"api/work-orders/{workOrderId}/attachments/{attachmentId}/file" + (inline ? "?inline=1" : "");
}

public sealed class HttpWorkOrderAttachmentStore : IWorkOrderAttachmentStore
{
    // Scanned quotes and photos are a few MB; the browser posts straight to the API, so the
    // practical ceiling is the Functions request size — the server enforces the same 64 MB cap.
    private const long MaxUploadBytes = 64L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpWorkOrderAttachmentStore(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<WorkOrderAttachment>> ListAsync(string workOrderId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListWorkOrderAttachments(workOrderId), cancellationToken);

    public async Task<IReadOnlyList<WorkOrderAttachment>> UploadFilesAsync(
        string workOrderId, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
        }

        var response = await httpClient.PostAsync($"api/work-orders/{workOrderId}/attachments", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface the server's own message (storage misconfigured, file too large) rather than
            // a bare status code — these are the failures a user can actually do something about.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        var updated = await response.Content.ReadFromJsonAsync<List<WorkOrderAttachment>>(cancellationToken: cancellationToken);
        return updated ?? await ListAsync(workOrderId, cancellationToken);
    }

    public Task<IReadOnlyList<WorkOrderAttachment>> RemoveAsync(
        string workOrderId, string attachmentId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RemoveWorkOrderAttachment(workOrderId, attachmentId), cancellationToken);
}
