using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

public sealed class HttpSitePhotoStore : ISitePhotoStore
{
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
        PhotoUploads.AddFiles(content, files, cancellationToken);

        var response = await httpClient.PostAsync("api/site-photos", content, cancellationToken);
        await PhotoUploads.ThrowIfFailedAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<SitePhotoUploadResult>(cancellationToken: cancellationToken))!;
    }

    public Task RestoreAsync(string sitePhotoId, CancellationToken cancellationToken) =>
        commands.SendAsync(new RestoreSitePhoto(sitePhotoId), cancellationToken);

    public Task DeleteAsync(string sitePhotoId, CancellationToken cancellationToken) =>
        commands.SendAsync(new DeleteSitePhoto(sitePhotoId), cancellationToken);
}
