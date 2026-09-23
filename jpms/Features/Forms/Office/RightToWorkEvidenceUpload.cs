using System.Net.Http.Headers;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// The checker's evidence posted to the restricted right-to-work store against the check — a raw
/// multipart post, as every file upload in the portal is. Answers the sentence to show when it did
/// not go, or null when it did.
/// </summary>
public static class RightToWorkEvidenceUpload
{
    private const long LargestEvidence = 25L * 1024 * 1024;
    private const string AnyFile = "application/octet-stream";
    private const string TooLarge = "The evidence file must be under 25 MB.";

    public static async Task<string?> SendAsync(HttpClient http, string rightToWorkCheckId, IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(LargestEvidence));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? AnyFile : file.ContentType);
            content.Add(fileContent, "file", file.Name);
            using var response = await http.PostAsync($"api/right-to-work-checks/{Uri.EscapeDataString(rightToWorkCheckId)}/evidence", content);
            return response.IsSuccessStatusCode ? null : (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
        }
        catch (IOException) { return TooLarge; }
    }
}
