using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.Ai.Storage;

/// <summary>
/// Azure Blob Storage implementation. Blobs are keyed
/// <c>conversations/{conversationId}/{attachmentId}/{fileName}</c> in a single private container
/// created on first use. Mirrors AzureBlobDocumentControlStore — bounded retries so an unreachable
/// account fails fast inside a chat hop rather than sitting on the SDK's default retry chain.
/// Retention is a lifecycle rule on the container (infra/run-ai-attachments-lifecycle.sh), not
/// code: a read after the rule has fired gets null from <see cref="OpenAsync"/> and the tool says
/// so.
/// </summary>
public sealed class AzureBlobAiAttachmentStore : IAiAttachmentStore
{
    public const string ContainerName = "ai-attachments";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobAiAttachmentStore(string connectionString)
    {
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

    public bool IsConfigured => true;

    public async Task<string> UploadAsync(
        string conversationId, string attachmentId, string fileName, string contentType,
        byte[] content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var blobRef = $"conversations/{conversationId}/{attachmentId}/{SafeName(fileName)}";
        var blob = container.GetBlobClient(blobRef);
        using var stream = new MemoryStream(content, writable: false);
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<byte[]?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadContentAsync(cancellationToken);
        return download.Value.Content.ToArray();
    }

    public async Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static string SafeName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeName) ? "file" : safeName;
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

/// <summary>No store configured: uploads are refused with the reason, reads find nothing.</summary>
public sealed class NullAiAttachmentStore : IAiAttachmentStore
{
    public bool IsConfigured => false;

    public Task<string> UploadAsync(
        string conversationId, string attachmentId, string fileName, string contentType,
        byte[] content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "Attachment storage is not configured for this API (AiAttachmentStorage:ConnectionString, "
            + "or the shared storage account) — the assistant cannot keep files to read from.");

    public Task<byte[]?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<byte[]?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
