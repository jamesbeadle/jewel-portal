using System.Net.Http.Headers;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

/// <summary>
/// The multipart/streaming edge of building control files — uploads from this computer (or the
/// phone's browser, on site) and the proxied download URLs. Listing rides the tab's one view
/// query (BuildingControlReadModel); removal and re-kind go through the command routes.
/// Deliberately not cached — the tender-enquiry attachment store's reasoning.
/// </summary>
public interface IBuildingControlAttachmentClient
{
    /// <summary>Uploads files onto the case (kind names what they are — Notice, Decision notice,
    /// Completion certificate…).</summary>
    Task UploadToCaseAsync(
        string caseId, BuildingControlAttachmentKind kind, IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads files onto an inspection. Kind is usually inferred server-side (images →
    /// Photo, PDFs → Site inspection report); pass one to override.</summary>
    Task UploadToInspectionAsync(
        string inspectionId, BuildingControlAttachmentKind? kind, IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams a stored file; <paramref name="isInline"/> renders it in
    /// place (the photo grid's thumbnails), otherwise the browser downloads it.</summary>
    string FileUrl(string attachmentId, bool isInline = false) =>
        $"api/building-control/attachments/{attachmentId}/file" + (isInline ? "?inline=1" : "");
}

public sealed class HttpBuildingControlAttachmentClient : IBuildingControlAttachmentClient
{
    // The browser posts straight to the API; the server enforces the same 64 MB cap.
    private const long MaxUploadBytes = 64L * 1024 * 1024;

    private readonly HttpClient httpClient;

    public HttpBuildingControlAttachmentClient(HttpClient httpClient) { this.httpClient = httpClient; }

    public Task UploadToCaseAsync(
        string caseId, BuildingControlAttachmentKind kind, IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default) =>
        UploadAsync($"api/building-control/cases/{caseId}/attachments?kind={(int)kind}", files, cancellationToken);

    public Task UploadToInspectionAsync(
        string inspectionId, BuildingControlAttachmentKind? kind, IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default) =>
        UploadAsync(
            $"api/building-control/inspections/{inspectionId}/attachments" + (kind is { } k ? $"?kind={(int)k}" : ""),
            files, cancellationToken);

    private async Task UploadAsync(string url, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
        }

        var response = await httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // The server's own message (storage misconfigured, file too large) rather than a bare
            // status code — these are the failures a user can actually do something about.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }
    }
}
