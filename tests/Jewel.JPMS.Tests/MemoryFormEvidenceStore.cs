using Jewel.JPMS.Api.Features.Forms.Storage;
using Jewel.JPMS.Api.Storage;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Tests;

/// <summary>The forms' three stores held in memory, so a test can see which store a file landed in.</summary>
internal sealed class MemoryFormEvidenceStore : IFormEvidenceStore
{
    private readonly Dictionary<(FormEvidenceStore Store, string BlobRef), byte[]> files = new();

    public bool IsConfigured => true;

    public bool Holds(FormEvidenceStore store, string blobRef) => files.ContainsKey((store, blobRef));

    public Task SaveAsync(FormEvidenceStore store, string blobRef, string contentType, byte[] bytes, CancellationToken cancellationToken)
    {
        files[(store, blobRef)] = bytes;
        return Task.CompletedTask;
    }

    public Task<StoredBlob?> OpenAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken)
    {
        var isHeld = files.TryGetValue((store, blobRef), out var bytes);
        var blob = isHeld ? new StoredBlob(new MemoryStream(bytes!), "application/octet-stream", bytes!.LongLength) : null;
        return Task.FromResult<StoredBlob?>(blob);
    }

    public Task<bool> DeleteAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult(files.Remove((store, blobRef)));
}
