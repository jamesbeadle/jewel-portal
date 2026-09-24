using System.Net.Http.Headers;
using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Features.Registers.Policies;

/// <summary>
/// A policy revision's PDF, attached from the Policies page (multipart, as every file the office
/// uploads) and read back by its address. The api refuses a new file once anyone has signed the revision.
/// </summary>
public sealed class PolicyFileUpload
{
    private const long LargestBytes = 25L * 1024 * 1024;
    private const string PdfType = "application/pdf";

    private readonly HttpClient http;

    public PolicyFileUpload(HttpClient http) { this.http = http; }

    public static string FileAddress(string policyDocumentId) => $"/api/registers/policies/{Uri.EscapeDataString(policyDocumentId)}/file";

    public async Task AttachAsync(string policyDocumentId, IBrowserFile file, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(LargestBytes, cancellationToken));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(PdfType);
        content.Add(fileContent, "file", file.Name);
        using var response = await http.PostAsync(FileAddress(policyDocumentId).TrimStart('/'), content, cancellationToken);
        if (response.IsSuccessStatusCode) return;
        var sentence = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim().Trim('[', ']').Trim('"');
        throw new InvalidOperationException(sentence.Length > 0 ? sentence : "The PDF didn't attach — try again.");
    }
}
