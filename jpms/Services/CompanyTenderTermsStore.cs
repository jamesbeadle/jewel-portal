using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

/// <summary>What is uploaded as the company's standard tender Terms &amp; Conditions right now.
/// Exists=false with Configured=false means no file storage is set up on the API, which the
/// admin panel says out loud instead of offering an upload that cannot land.</summary>
public sealed record CompanyTenderTermsStatus(
    bool Exists,
    bool Configured,
    string? FileName,
    long FileSizeBytes,
    DateTimeOffset? UploadedAt);

/// <summary>
/// The company's standard tender Terms &amp; Conditions PDF — one document, company-wide, attached
/// automatically to every tender invite email. Uploaded/replaced from Admin → System.
/// </summary>
public interface ICompanyTenderTermsStore
{
    Task<CompanyTenderTermsStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<CompanyTenderTermsStatus> UploadAsync(IBrowserFile file, CancellationToken cancellationToken = default);

    /// <summary>The API URL that downloads the stored PDF.</summary>
    string FileUrl => "api/company/tender-terms/file";
}

public sealed class HttpCompanyTenderTermsStore : ICompanyTenderTermsStore
{
    private const long MaxUploadBytes = 10L * 1024 * 1024;

    private readonly HttpClient httpClient;

    public HttpCompanyTenderTermsStore(HttpClient httpClient) { this.httpClient = httpClient; }

    public async Task<CompanyTenderTermsStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<CompanyTenderTermsStatus>("api/company/tender-terms", cancellationToken)
            ?? new CompanyTenderTermsStatus(false, false, null, 0, null);

    public async Task<CompanyTenderTermsStatus> UploadAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await httpClient.PostAsync("api/company/tender-terms", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body) ? $"Server returned {(int)response.StatusCode}." : body.Trim('"'));
        }

        return await response.Content.ReadFromJsonAsync<CompanyTenderTermsStatus>(cancellationToken: cancellationToken)
            ?? await GetStatusAsync(cancellationToken);
    }
}
