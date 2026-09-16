using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

public sealed class HttpSitePhotoStore : ISitePhotoStore
{
    // Phone photos run 5–15 MB; this bounds a single file, not the batch.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpSitePhotoStore(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<SitePhoto>> ListAsync(CancellationToken cancellationToken) =>
        queries.AskAsync(new ListSitePhotos(), cancellationToken);

    public async Task<SitePhotoUploadResult> UploadAsync(IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "files", file.Name);
        }

        var response = await httpClient.PostAsync("api/site-photos", content, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<SitePhotoUploadResult>(cancellationToken: cancellationToken))!;
    }

    public Task DeleteAsync(string sitePhotoId, CancellationToken cancellationToken) =>
        commands.SendAsync(new DeleteSitePhoto(sitePhotoId), cancellationToken);

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
    }
}
