using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

/// <summary>
/// The files kept on a tender enquiry — the questionnaire, the drawings, supporting material.
/// Deliberately not cached: the lists are short and change as people add to them, same reasoning
/// as the bid-package attachment store.
/// </summary>
public interface ITenderEnquiryAttachmentStore
{
    Task<IReadOnlyList<TenderEnquiryAttachment>> ListAsync(string tenderEnquiryId, CancellationToken cancellationToken = default);

    /// <summary>Uploads one or more files from this computer onto the enquiry.</summary>
    Task<IReadOnlyList<TenderEnquiryAttachment>> UploadFilesAsync(
        string tenderEnquiryId, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenderEnquiryAttachment>> RemoveAsync(
        string tenderEnquiryId, string attachmentId, CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams a stored file; <paramref name="isInline"/> renders it in
    /// place, otherwise the browser downloads it.</summary>
    string FileUrl(string tenderEnquiryId, string attachmentId, bool isInline = false) =>
        $"api/tender-enquiries/{tenderEnquiryId}/attachments/{attachmentId}/file" + (isInline ? "?inline=1" : "");
}

public sealed class HttpTenderEnquiryAttachmentStore : ITenderEnquiryAttachmentStore
{
    // The browser posts straight to the API; the server enforces the same 64 MB cap.
    private const long MaxUploadBytes = 64L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpTenderEnquiryAttachmentStore(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<TenderEnquiryAttachment>> ListAsync(string tenderEnquiryId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTenderEnquiryAttachments(tenderEnquiryId), cancellationToken);

    public async Task<IReadOnlyList<TenderEnquiryAttachment>> UploadFilesAsync(
        string tenderEnquiryId, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
        }

        var response = await httpClient.PostAsync($"api/tender-enquiries/{tenderEnquiryId}/attachments", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // The server's own message (storage misconfigured, file too large) rather than a bare
            // status code — these are the failures a user can actually do something about.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        var updated = await response.Content.ReadFromJsonAsync<List<TenderEnquiryAttachment>>(cancellationToken: cancellationToken);
        return updated ?? await ListAsync(tenderEnquiryId, cancellationToken);
    }

    public Task<IReadOnlyList<TenderEnquiryAttachment>> RemoveAsync(
        string tenderEnquiryId, string attachmentId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RemoveTenderEnquiryAttachment(tenderEnquiryId, attachmentId), cancellationToken);
}
