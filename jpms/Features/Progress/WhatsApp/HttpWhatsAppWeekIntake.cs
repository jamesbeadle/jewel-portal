using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Progress.WhatsApp;

/// <summary>Sends the export as multipart/form-data — a zip can run to hundreds of megabytes, so
/// it never goes through the JSON command sender — and refreshes the progress feed after a write.</summary>
public sealed class HttpWhatsAppWeekIntake : IWhatsAppWeekIntake
{
    private const long MaxExportBytes = 200L * 1024 * 1024;
    private const string DateFormat = "yyyy-MM-dd";

    private readonly HttpClient httpClient;
    private readonly IProgressStore progressStore;

    public HttpWhatsAppWeekIntake(HttpClient httpClient, IProgressStore progressStore)
    {
        this.httpClient = httpClient;
        this.progressStore = progressStore;
    }

    public async Task<WhatsAppWeekPreview> PreviewAsync(
        string projectId, DateOnly weekEnding, string chatText, IBrowserFile? export, CancellationToken cancellationToken)
    {
        using var content = Form(weekEnding, chatText, export, Array.Empty<DateOnly>(), cancellationToken);
        var response = await httpClient.PostAsync($"api/projects/{projectId}/progress/whatsapp-week/preview", content, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<WhatsAppWeekPreview>(cancellationToken: cancellationToken))!;
    }

    public async Task<WhatsAppWeekApplied> ApplyAsync(
        string projectId, DateOnly weekEnding, string chatText, IBrowserFile? export,
        IReadOnlyList<DateOnly> days, CancellationToken cancellationToken)
    {
        using var content = Form(weekEnding, chatText, export, days, cancellationToken);
        var response = await httpClient.PostAsync($"api/projects/{projectId}/progress/whatsapp-week/apply", content, cancellationToken);
        await ThrowIfFailedAsync(response, cancellationToken);
        progressStore.Refresh(projectId);
        return (await response.Content.ReadFromJsonAsync<WhatsAppWeekApplied>(cancellationToken: cancellationToken))!;
    }

    private static MultipartFormDataContent Form(
        DateOnly weekEnding, string chatText, IBrowserFile? export, IReadOnlyList<DateOnly> days, CancellationToken cancellationToken)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(weekEnding.ToString(DateFormat, CultureInfo.InvariantCulture)), "weekEnding");
        content.Add(new StringContent(string.Join(",", days.Select(day => day.ToString(DateFormat, CultureInfo.InvariantCulture)))), "days");
        if (export is null)
        {
            content.Add(new StringContent(chatText), "chatText");
            return content;
        }
        var file = new StreamContent(export.OpenReadStream(MaxExportBytes, cancellationToken));
        file.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(export.ContentType) ? "application/octet-stream" : export.ContentType);
        content.Add(file, "export", export.Name);
        return content;
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
    }
}
