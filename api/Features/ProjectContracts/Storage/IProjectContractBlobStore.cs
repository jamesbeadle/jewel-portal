namespace Jewel.JPMS.Api.Features.ProjectContracts.Storage;

public interface IProjectContractBlobStore
{
    Task<string> UploadAsync(
        string projectId, string projectContractId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    /// <summary>Stores an amendment document under {projectId}/amendments/{amendmentId}/ — the
    /// same container as the executed contract, a path segment apart, so one storage setting
    /// covers both.</summary>
    Task<string> UploadAmendmentAsync(
        string projectId, string projectContractAmendmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<ProjectContractBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record ProjectContractBlob(Stream Content, string ContentType, long Length);
