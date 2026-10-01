using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Features.Progress;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>Puts the day's photographs onto the note the worker's own sign-out wrote — the worker's
/// door onto the project's progress feed, which only accepts their own note.</summary>
public sealed class MyDayPhotoUploader
{
    public const int MaxPerBatch = 30;

    private readonly HttpClient httpClient;
    public MyDayPhotoUploader(HttpClient httpClient) { this.httpClient = httpClient; }

    public Task<ProgressPhotoBatchResult> UploadAsync(
        string progressUpdateId, IReadOnlyList<IBrowserFile> photos, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        PhotoUploads.AddFiles(content, photos, cancellationToken);
        return PostAsync(progressUpdateId, content, cancellationToken);
    }

    public Task<ProgressPhotoBatchResult> UploadAsync(
        string progressUpdateId, IReadOnlyList<ChosenPhoto> photos, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        PhotoUploads.AddFiles(content, photos);
        return PostAsync(progressUpdateId, content, cancellationToken);
    }

    private async Task<ProgressPhotoBatchResult> PostAsync(string progressUpdateId, MultipartFormDataContent content, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync($"api/my/labour/notes/{progressUpdateId}/photos", content, cancellationToken);
        await PhotoUploads.ThrowIfFailedAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ProgressPhotoBatchResult>(cancellationToken: cancellationToken))!;
    }
}
