namespace Jewel.JPMS.Api.Features.Subcontractors.Storage;

/// <summary>
/// Fallback used when no storage connection string is configured. Any attempt to store or read a
/// file fails loudly so a misconfigured environment is obvious rather than silently losing files.
/// </summary>
public sealed class NullComplianceBlobStore : IComplianceBlobStore
{
    private const string Message =
        "Compliance document storage is not configured. Set 'ComplianceStorage:ConnectionString' (or 'AzureWebJobsStorage').";

    public Task<string> UploadAsync(
        string subcontractorId, string complianceDocumentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task<ComplianceBlob?> OpenAsync(string blobPath, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task DeleteAsync(string blobPath, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);
}
