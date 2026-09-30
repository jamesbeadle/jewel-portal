using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Progress;

/// <summary>How a batch of photographs leaves the phone: each file one part of a multipart form under
/// the field every photo intake reads, bounded per file rather than per batch because phone photos
/// run 5–15 MB; and how a refused upload reads back as the server's own sentence, never a bare
/// status code.</summary>
public static class PhotoUploads
{
    private const long MaxUploadBytes = 100L * 1024 * 1024;
    private const string FilesField = "files";
    private const string UnknownContentType = "application/octet-stream";

    public static void AddFiles(MultipartFormDataContent content, IReadOnlyList<IBrowserFile> photos, CancellationToken cancellationToken)
    {
        foreach (var photo in photos)
        {
            var fileContent = new StreamContent(photo.OpenReadStream(MaxUploadBytes, cancellationToken));
            var contentType = string.IsNullOrWhiteSpace(photo.ContentType) ? UnknownContentType : photo.ContentType;
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, FilesField, photo.Name);
        }
    }

    public static void AddFiles(MultipartFormDataContent content, IReadOnlyList<ChosenPhoto> photos)
    {
        foreach (var photo in photos)
        {
            var fileContent = new ByteArrayContent(photo.Bytes);
            var contentType = string.IsNullOrWhiteSpace(photo.ContentType) ? UnknownContentType : photo.ContentType;
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, FilesField, photo.Name);
        }
    }

    public static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var isBodiless = string.IsNullOrWhiteSpace(body);
        throw new InvalidOperationException(isBodiless ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
    }
}
