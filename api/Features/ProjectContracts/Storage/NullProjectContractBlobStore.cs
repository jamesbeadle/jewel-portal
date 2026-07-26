namespace Jewel.JPMS.Api.Features.ProjectContracts.Storage;

/// <summary>Registered when no storage connection string is configured, so the failure names the
/// setting to fix rather than surfacing as a null reference.</summary>
public sealed class NullProjectContractBlobStore : IProjectContractBlobStore
{
    private const string Message =
        "Contract document storage is not configured. Set 'ProjectContractsStorage:ConnectionString' (or 'AzureWebJobsStorage').";

    public Task<string> UploadAsync(
        string projectId, string projectContractId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task<ProjectContractBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);
}
