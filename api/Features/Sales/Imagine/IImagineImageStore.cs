using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// Where the imagine photos and renders live: the private "imagine" blob container, keyed
/// <c>{leadId}/{roundId}/{imageId}.{ext}</c>. Bytes are only ever served through the API
/// (the public endpoint checks the lead's token; the staff endpoint the signed-in user) — the
/// container is never public and no SAS is minted.
/// </summary>
public interface IImagineImageStore
{
    bool IsConfigured { get; }
    Task<string> SaveAsync(string leadId, string roundId, string imageId, string contentType, byte[] bytes, CancellationToken ct);
    Task<StoredBlob?> OpenAsync(string blobRef, CancellationToken ct);
    Task<byte[]?> ReadAllAsync(string blobRef, CancellationToken ct);
}

public sealed class AzureBlobImagineImageStore : IImagineImageStore
{
    public const string ContainerName = "imagine";
    private readonly AzureBlobFileStore store;

    public AzureBlobImagineImageStore(string connectionString)
    {
        store = new AzureBlobFileStore(connectionString, ContainerName);
    }

    public bool IsConfigured => true;

    public async Task<string> SaveAsync(string leadId, string roundId, string imageId, string contentType, byte[] bytes, CancellationToken ct)
    {
        var blobRef = $"{leadId}/{roundId}/{imageId}.{Extension(contentType)}";
        using var stream = new MemoryStream(bytes, writable: false);
        await store.UploadAsync(blobRef, contentType, stream, ct);
        return blobRef;
    }

    public Task<StoredBlob?> OpenAsync(string blobRef, CancellationToken ct) => store.OpenAsync(blobRef, ct);

    public async Task<byte[]?> ReadAllAsync(string blobRef, CancellationToken ct)
    {
        var blob = await store.OpenAsync(blobRef, ct);
        if (blob is null) return null;
        await using var content = blob.Content;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    public static string Extension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => "png",
        "image/webp" => "webp",
        _ => "jpg"
    };
}

/// <summary>No storage configured: every call says so, so a misconfiguration reads as a message
/// on the page rather than a silent empty gallery.</summary>
public sealed class NullImagineImageStore : IImagineImageStore
{
    private const string Reason = "Image storage isn't configured (ImagineStorage:ConnectionString / DrawingsStorage:ConnectionString).";

    public bool IsConfigured => false;

    public Task<string> SaveAsync(string leadId, string roundId, string imageId, string contentType, byte[] bytes, CancellationToken ct) =>
        Task.FromException<string>(new InvalidOperationException(Reason));

    public Task<StoredBlob?> OpenAsync(string blobRef, CancellationToken ct) => Task.FromResult<StoredBlob?>(null);

    public Task<byte[]?> ReadAllAsync(string blobRef, CancellationToken ct) => Task.FromResult<byte[]?>(null);
}
