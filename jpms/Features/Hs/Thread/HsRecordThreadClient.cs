using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs.Thread;

/// <summary>
/// The thread on a record as the page reads and writes it: the comments, a comment from words
/// alone through the command route, and a comment with photographs through the multipart door.
/// </summary>
public sealed class HsRecordThreadClient
{
    private const long MaxUploadBytes = 100L * 1024 * 1024;
    private const string TextField = "text";
    private const string FilesField = "files";

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HsRecordThreadClient(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<HsRecordComment>> ThreadAsync(string hsRecordId, CancellationToken cancellationToken) =>
        queries.AskAsync(new ListHsRecordComments(hsRecordId), cancellationToken);

    public Task<HsRecordComment> CommentAsync(string hsRecordId, string text, CancellationToken cancellationToken) =>
        commands.SendAsync(new CommentOnHsRecord(hsRecordId, text), cancellationToken);

    public async Task<HsRecordCommentWithPhotos> CommentWithPhotosAsync(
        string hsRecordId, string text, IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(text), TextField);
        foreach (var file in files) content.Add(FileContentOf(file, cancellationToken), FilesField, file.Name);
        var response = await httpClient.PostAsync($"api/hs-records/{hsRecordId}/comments/with-photos", content, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<HsRecordCommentWithPhotos>(cancellationToken: cancellationToken))!;
    }

    private static StreamContent FileContentOf(IBrowserFile file, CancellationToken cancellationToken)
    {
        var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return fileContent;
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
    }
}
