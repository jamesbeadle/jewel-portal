using System.Net.Http.Headers;
using System.Text;

namespace Jewel.JPMS.Api.Features.Bluebeam;

// The session-workflow half of BluebeamClient: create session → add file slot → PUT bytes to the
// slot's AWS URL → confirm → read markups → finalise → delete. Paths and payloads verified against
// the live developer-portal OpenAPI document (2026-08-31): sessions are /publicapi/v1, the markups
// read is /publicapi/v2; every call carries the bearer token AND the client_id header.
public sealed partial class BluebeamClient
{
    public async Task<string> CreateSessionAsync(
        string accessToken, string sessionName, CancellationToken cancellationToken)
    {
        var payload = new { Name = sessionName, Notification = false, Restricted = true };
        var body = await SendAsync(HttpMethod.Post, "/publicapi/v1/sessions", accessToken, JsonBody(payload), cancellationToken);
        using var document = JsonDocument.Parse(body);
        return ReadString(document.RootElement, "Id", "SessionId")
            ?? throw new BluebeamCallFailedException("Bluebeam's create-session response contained no session id.");
    }

    public async Task<BluebeamFileSlot> AddSessionFileAsync(
        string accessToken, string sessionId, string fileName, long fileSizeBytes, CancellationToken cancellationToken)
    {
        // SessionFileCreateDto: Name and Source are required; Size helps Bluebeam validate.
        var payload = new { Name = fileName, Source = "JPMS", Size = fileSizeBytes };
        var body = await SendAsync(
            HttpMethod.Post, $"/publicapi/v1/sessions/{sessionId}/files", accessToken, JsonBody(payload), cancellationToken);
        using var document = JsonDocument.Parse(body);
        var fileId = ReadString(document.RootElement, "Id", "FileId")
            ?? throw new BluebeamCallFailedException("Bluebeam's add-file response contained no file id.");
        var uploadUrl = ReadString(document.RootElement, "UploadUrl", "UploadURL")
            ?? throw new BluebeamCallFailedException("Bluebeam's add-file response contained no upload URL.");
        var uploadContentType = ReadString(document.RootElement, "UploadContentType") ?? "application/pdf";
        return new BluebeamFileSlot(fileId, uploadUrl, uploadContentType);
    }

    public async Task UploadFileBytesAsync(BluebeamFileSlot slot, byte[] pdfBytes, CancellationToken cancellationToken)
    {
        // The upload URL is a pre-signed AWS PUT — no bearer, but the encryption header and the
        // exact returned Content-Type are mandatory or S3 refuses the signature.
        using var request = new HttpRequestMessage(HttpMethod.Put, slot.UploadUrl)
        {
            Content = new ByteArrayContent(pdfBytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(slot.UploadContentType);
        request.Headers.Add("x-amz-server-side-encryption", "AES256");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return;
        throw new BluebeamCallFailedException(
            $"The PDF upload to Bluebeam's storage failed with HTTP {(int)response.StatusCode}.");
    }

    public async Task ConfirmUploadAsync(
        string accessToken, string sessionId, string fileId, CancellationToken cancellationToken) =>
        await SendAsync(
            HttpMethod.Post, $"/publicapi/v1/sessions/{sessionId}/files/{fileId}/confirm-upload",
            accessToken, content: null, cancellationToken);

    public Task<string> GetMarkupsRawJsonAsync(
        string accessToken, string sessionId, string fileId, CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Get, $"/publicapi/v2/sessions/{sessionId}/files/{fileId}/markups",
            accessToken, content: null, cancellationToken);

    public async Task FinalizeSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken) =>
        await SendAsync(
            HttpMethod.Put, $"/publicapi/v1/sessions/{sessionId}", accessToken,
            JsonBody(new { Status = "Finalizing" }), cancellationToken);

    public async Task DeleteSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken) =>
        await SendAsync(
            HttpMethod.Delete, $"/publicapi/v1/sessions/{sessionId}", accessToken, content: null, cancellationToken);

    public async Task<BluebeamUser> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        var body = await SendAsync(HttpMethod.Get, "/publicapi/v1/users/me", accessToken, content: null, cancellationToken);
        using var document = JsonDocument.Parse(body);
        var email = ReadString(document.RootElement, "Email") ?? "";
        var displayName = ReadString(document.RootElement, "Name", "DisplayName") ?? email;
        return new BluebeamUser(email, displayName);
    }

    private async Task<string> SendAsync(
        HttpMethod method, string path, string accessToken, HttpContent? content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{options.ApiBaseUrl}{path}") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("client_id", options.ClientId);
        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode) return body;
        logger.LogWarning("Bluebeam call {Method} {Path} failed: {Status} {Body}.", method, path, (int)response.StatusCode, Trimmed(body));
        throw new BluebeamCallFailedException(
            $"Bluebeam refused {method} {path} with HTTP {(int)response.StatusCode}. {Trimmed(body)}");
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
}
