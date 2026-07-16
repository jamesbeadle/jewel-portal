using System.Globalization;
using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public sealed class HttpProgressStore : IProgressStore
{
    // Phone photos run 5–15 MB; this bounds a single file, not the batch.
    private const long MaxUploadBytes = 100L * 1024 * 1024;

    private readonly ProgressReadModel readModel;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpProgressStore(ProgressReadModel readModel, ICommandSender commands, HttpClient httpClient)
    {
        this.readModel = readModel;
        this.commands = commands;
        this.httpClient = httpClient;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public bool UpdatesLoadedFor(string projectId) => readModel.UpdatesLoaded(projectId);

    public IReadOnlyList<ProgressUpdate> UpdatesFor(string projectId)
    {
        readModel.EnsureUpdates(projectId, CancellationToken.None);
        return readModel.UpdatesCurrent(projectId);
    }

    public IReadOnlyList<ProgressReport> ReportsFor(string projectId)
    {
        readModel.EnsureReports(projectId, CancellationToken.None);
        return readModel.ReportsCurrent(projectId);
    }

    // Forces a background reload even when cached. Pages call this once on entry (never from
    // render) so tab navigation picks up changes made elsewhere (stale-while-revalidate).
    public void Refresh(string projectId) => RefreshInBackground(projectId);

    public async Task CreateUpdateAsync(
        string projectId, string title, string description, DateTimeOffset? workDate,
        ProgressWeather? weather, IReadOnlyList<IBrowserFile> photos, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "title");
        content.Add(new StringContent(description), "description");
        if (workDate is { } date) content.Add(new StringContent(date.ToString("O")), "workDate");
        AddWeatherFields(content, weather);
        AddFiles(content, photos, cancellationToken);

        var response = await httpClient.PostAsync($"api/projects/{projectId}/progress-updates", content, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);

        // The write has been committed. Refresh caches in the background so a slow or stalled
        // refresh cannot keep the upload UI stuck on "Uploading…".
        RefreshInBackground(projectId);
    }

    public async Task AddPhotosAsync(
        string projectId, string progressUpdateId,
        IReadOnlyList<IBrowserFile> photos, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        AddFiles(content, photos, cancellationToken);

        var response = await httpClient.PostAsync($"api/progress-updates/{progressUpdateId}/photos", content, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        RefreshInBackground(projectId);
    }

    public async Task UpdateUpdateAsync(
        string projectId, string progressUpdateId, string title, string description,
        DateTimeOffset? workDate, ProgressWeather? weather, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new UpdateProgressUpdate(progressUpdateId, title, description, workDate, weather), cancellationToken);
        RefreshInBackground(projectId);
    }

    public async Task DeleteUpdateAsync(string projectId, string progressUpdateId, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new DeleteProgressUpdate(progressUpdateId), cancellationToken);
        RefreshInBackground(projectId);
    }

    public async Task DeletePhotoAsync(string projectId, string progressUpdateId, string progressPhotoId, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new DeleteProgressPhoto(progressUpdateId, progressPhotoId), cancellationToken);
        RefreshInBackground(projectId);
    }

    public async Task<ProgressReport> CreateReportAsync(
        string projectId, string title, DateTimeOffset? periodStart, DateTimeOffset? periodEnd,
        string introduction, string workCompleted, string upcomingWorks,
        IReadOnlyList<string> selectedUpdateIds, CancellationToken cancellationToken)
    {
        // The API sets the creator from the signed-in user; the email here is ignored server-side.
        var report = await commands.SendAsync(
            new CreateProgressReport(projectId, string.Empty, title, periodStart, periodEnd,
                introduction, workCompleted, upcomingWorks, selectedUpdateIds), cancellationToken);
        RefreshInBackground(projectId);
        return report;
    }

    public async Task<ProgressReport> UpdateReportAsync(
        string projectId, string progressReportId, string title, DateTimeOffset? periodStart,
        DateTimeOffset? periodEnd, string introduction, string workCompleted, string upcomingWorks,
        IReadOnlyList<string> selectedUpdateIds, CancellationToken cancellationToken)
    {
        var report = await commands.SendAsync(
            new UpdateProgressReport(progressReportId, title, periodStart, periodEnd,
                introduction, workCompleted, upcomingWorks, selectedUpdateIds), cancellationToken);
        RefreshInBackground(projectId);
        return report;
    }

    public async Task DeleteReportAsync(string projectId, string progressReportId, CancellationToken cancellationToken)
    {
        await commands.SendAsync(new DeleteProgressReport(progressReportId), cancellationToken);
        RefreshInBackground(projectId);
    }

    // Weather values are formatted invariant — the API parses them invariant too, so a site
    // manager's locale can never change what gets recorded.
    private static void AddWeatherFields(MultipartFormDataContent content, ProgressWeather? weather)
    {
        if (weather is null) return;
        if (!string.IsNullOrWhiteSpace(weather.Summary)) content.Add(new StringContent(weather.Summary.Trim()), "weatherSummary");
        if (weather.ObservedAt is { } observedAt) content.Add(new StringContent(observedAt.ToString("O")), "weatherObservedAt");
        if (weather.TempHighC is { } highC) content.Add(new StringContent(highC.ToString(CultureInfo.InvariantCulture)), "weatherTempHighC");
        if (weather.TempLowC is { } lowC) content.Add(new StringContent(lowC.ToString(CultureInfo.InvariantCulture)), "weatherTempLowC");
        if (weather.WindMph is { } windMph) content.Add(new StringContent(windMph.ToString(CultureInfo.InvariantCulture)), "weatherWindMph");
        if (weather.HumidityPercent is { } humidity) content.Add(new StringContent(humidity.ToString(CultureInfo.InvariantCulture)), "weatherHumidityPercent");
        if (weather.PrecipInches is { } precip) content.Add(new StringContent(precip.ToString(CultureInfo.InvariantCulture)), "weatherPrecipInches");
    }

    private static void AddFiles(MultipartFormDataContent content, IReadOnlyList<IBrowserFile> photos, CancellationToken cancellationToken)
    {
        foreach (var photo in photos)
        {
            var fileContent = new StreamContent(photo.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(photo.ContentType) ? "application/octet-stream" : photo.ContentType);
            content.Add(fileContent, "files", photo.Name);
        }
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        // Surface the server's message (e.g. a storage error) rather than a bare status code.
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
    }

    // Refreshes updates then reports without blocking the caller. Views update via
    // readModel.OnChanged when the refresh lands.
    private void RefreshInBackground(string projectId) => _ = RunRefreshAsync(projectId);

    private async Task RunRefreshAsync(string projectId)
    {
        try
        {
            await readModel.RefreshUpdatesAsync(projectId, CancellationToken.None);
            await readModel.RefreshReportsAsync(projectId, CancellationToken.None);
        }
        catch { /* OnChanged-driven views recover on the next interaction */ }
    }
}
