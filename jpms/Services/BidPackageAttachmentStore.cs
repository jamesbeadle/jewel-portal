using System.Net.Http.Headers;

namespace Jewel.JPMS.Services;

/// <summary>
/// Attachments kept on a bid package as tender documents — specification extracts, schedules of
/// finishes, survey photos. Supplier-facing: they go out with the invite email alongside the
/// linked drawings. Deliberately not cached — the lists are short and change as people add to
/// them, same reasoning as the work-order attachment store.
/// </summary>
public interface IBidPackageAttachmentStore
{
    Task<IReadOnlyList<BidPackageAttachment>> ListAsync(string bidPackageId, CancellationToken cancellationToken = default);

    /// <summary>Uploads one or more files from this computer onto the package.</summary>
    Task<IReadOnlyList<BidPackageAttachment>> UploadFilesAsync(
        string bidPackageId, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BidPackageAttachment>> RemoveAsync(
        string bidPackageId, string attachmentId, CancellationToken cancellationToken = default);

    /// <summary>The API URL that streams a stored file. <paramref name="inline"/> renders it in
    /// place (thumbnails and previews); otherwise the browser downloads it.</summary>
    string FileUrl(string bidPackageId, string attachmentId, bool inline = false) =>
        $"api/bid-packages/{bidPackageId}/attachments/{attachmentId}/file" + (inline ? "?inline=1" : "");
}

public sealed class HttpBidPackageAttachmentStore : IBidPackageAttachmentStore
{
    // Specification extracts and photos are a few MB; the browser posts straight to the API, so
    // the practical ceiling is the Functions request size — the server enforces the same 64 MB cap.
    private const long MaxUploadBytes = 64L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpBidPackageAttachmentStore(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<BidPackageAttachment>> ListAsync(string bidPackageId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListBidPackageAttachments(bidPackageId), cancellationToken);

    public async Task<IReadOnlyList<BidPackageAttachment>> UploadFilesAsync(
        string bidPackageId, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
        }

        var response = await httpClient.PostAsync($"api/bid-packages/{bidPackageId}/attachments", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface the server's own message (storage misconfigured, file too large, package
            // closed) rather than a bare status code — these are the failures a user can actually
            // do something about.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        var updated = await response.Content.ReadFromJsonAsync<List<BidPackageAttachment>>(cancellationToken: cancellationToken);
        return updated ?? await ListAsync(bidPackageId, cancellationToken);
    }

    public Task<IReadOnlyList<BidPackageAttachment>> RemoveAsync(
        string bidPackageId, string attachmentId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RemoveBidPackageAttachment(bidPackageId, attachmentId), cancellationToken);
}
