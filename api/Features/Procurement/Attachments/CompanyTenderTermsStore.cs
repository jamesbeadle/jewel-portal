using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

/// <summary>
/// The company's standard Terms &amp; Conditions PDF for tender invites — ONE document,
/// company-wide, uploaded in Admin → System and attached automatically to every invite email on
/// every project. Replacing it is uploading again; there is no history, because the current terms
/// are the only terms anyone should ever send.
/// </summary>
public interface ICompanyTenderTermsStore
{
    bool IsConfigured { get; }

    /// <summary>What is uploaded right now, or null when nothing is.</summary>
    Task<CompanyTenderTermsInfo?> GetInfoAsync(CancellationToken cancellationToken);

    /// <summary>The stored PDF's bytes and display name, or null when nothing is uploaded.</summary>
    Task<CompanyTenderTermsFile?> OpenAsync(CancellationToken cancellationToken);

    /// <summary>Stores (or replaces) the document.</summary>
    Task<CompanyTenderTermsInfo> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken);
}

public sealed record CompanyTenderTermsInfo(string FileName, long FileSizeBytes, DateTimeOffset UploadedAt);

public sealed record CompanyTenderTermsFile(byte[] Content, string FileName);

/// <summary>
/// Azure Blob Storage implementation: a fixed blob in a private company-documents container — no
/// database row, so no migration; existence of the blob IS the state. The original file name is
/// kept in blob metadata so the attachment reads as the company named it.
/// </summary>
public sealed class AzureBlobCompanyTenderTermsStore : ICompanyTenderTermsStore
{
    public const string ContainerName = "company-documents";
    private const string BlobName = "tender-terms.pdf";
    private const string FileNameMetadataKey = "originalname";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobCompanyTenderTermsStore(string connectionString)
    {
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Fixed,
                MaxRetries = 2,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(3),
                NetworkTimeout = TimeSpan.FromSeconds(30),
            }
        };
        container = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public bool IsConfigured => true;

    public async Task<CompanyTenderTermsInfo?> GetInfoAsync(CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(BlobName);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new CompanyTenderTermsInfo(
            FileNameFrom(properties.Value.Metadata),
            properties.Value.ContentLength,
            properties.Value.LastModified);
    }

    public async Task<CompanyTenderTermsFile?> OpenAsync(CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(BlobName);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadContentAsync(cancellationToken: cancellationToken);
        return new CompanyTenderTermsFile(
            download.Value.Content.ToArray(),
            FileNameFrom(download.Value.Details.Metadata));
    }

    public async Task<CompanyTenderTermsInfo> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = BlobName;

        var blob = container.GetBlobClient(BlobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" },
                // Metadata values must be ASCII — fall back to the fixed name if the original isn't.
                Metadata = new Dictionary<string, string>
                {
                    [FileNameMetadataKey] = safeName.All(char.IsAscii) ? safeName : BlobName
                }
            },
            cancellationToken);

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new CompanyTenderTermsInfo(safeName, properties.Value.ContentLength, properties.Value.LastModified);
    }

    private static string FileNameFrom(IDictionary<string, string> metadata) =>
        metadata.TryGetValue(FileNameMetadataKey, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name : BlobName;

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
/// No-storage fallback: invites simply go out without the terms (their absence is visible in the
/// Admin → System panel), but an UPLOAD fails loudly rather than pretending the document is kept.
/// </summary>
public sealed class NullCompanyTenderTermsStore : ICompanyTenderTermsStore
{
    public bool IsConfigured => false;

    public Task<CompanyTenderTermsInfo?> GetInfoAsync(CancellationToken cancellationToken) =>
        Task.FromResult<CompanyTenderTermsInfo?>(null);

    public Task<CompanyTenderTermsFile?> OpenAsync(CancellationToken cancellationToken) =>
        Task.FromResult<CompanyTenderTermsFile?>(null);

    public Task<CompanyTenderTermsInfo> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No file storage is configured, so the terms document can't be saved. " +
            "Set CompanyDocumentsStorage:ConnectionString (or AzureWebJobsStorage) and try again.");
}
