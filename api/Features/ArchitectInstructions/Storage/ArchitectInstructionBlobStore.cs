using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions.Storage;

/// <summary>
/// Stores and retrieves the document behind an Architect's Instruction. The container is private;
/// downloads are proxied through the API rather than handed out as public URLs — the same contract
/// the drawings store keeps.
/// </summary>
public interface IArchitectInstructionBlobStore
{
    /// <summary>Uploads an instruction's file and returns the blob reference to persist on the row.</summary>
    Task<string> UploadAsync(
        string projectId, string architectInstructionId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    /// <summary>Opens a stored file by its blob reference, or null if it no longer exists.</summary>
    Task<ArchitectInstructionBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    /// <summary>Deletes a stored file by its blob reference. Deleting a missing blob is a no-op.</summary>
    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record ArchitectInstructionBlob(Stream Content, string ContentType, long Length);

/// <summary>
/// Azure Blob Storage implementation. Files live in a single private container, keyed
/// <c>{projectId}/{architectInstructionId}/{fileName}</c>. The container is created on first use.
/// </summary>
public sealed class AzureBlobArchitectInstructionStore : IArchitectInstructionBlobStore
{
    public const string ContainerName = "architect-instructions";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobArchitectInstructionStore(string connectionString)
    {
        // Bounded retry/backoff, matching AzureBlobDrawingStore: a misconfigured storage account
        // should fail fast and visibly rather than making an upload appear to hang. NetworkTimeout
        // is per-attempt, so large files are unaffected.
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Fixed,
                MaxRetries = 2,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(3),
                NetworkTimeout = TimeSpan.FromSeconds(15),
            }
        };
        container = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public async Task<string> UploadAsync(
        string projectId, string architectInstructionId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "instruction";
        var blobRef = $"{projectId}/{architectInstructionId}/{safeName}";

        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<ArchitectInstructionBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        return new ArchitectInstructionBlob(
            download.Value.Content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            download.Value.Details.ContentLength);
    }

    public async Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (containerEnsured) return;
        await ensureContainerGate.WaitAsync(cancellationToken);
        try
        {
            if (!containerEnsured)
            {
                await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
                containerEnsured = true;
            }
        }
        finally
        {
            ensureContainerGate.Release();
        }
    }
}

/// <summary>
/// No-storage fallback for local development and any environment without a storage connection
/// string. Filing an instruction still records the row; the document simply cannot be stored, and
/// the register shows it as having no file rather than pretending the upload worked.
/// </summary>
public sealed class NullArchitectInstructionBlobStore : IArchitectInstructionBlobStore
{
    public Task<string> UploadAsync(
        string projectId, string architectInstructionId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No document storage is configured, so the instruction file can't be saved. " +
            "Set ArchitectInstructionsStorage:ConnectionString (or AzureWebJobsStorage) and try again.");

    public Task<ArchitectInstructionBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<ArchitectInstructionBlob?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
