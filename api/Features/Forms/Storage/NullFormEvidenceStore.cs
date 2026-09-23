using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Forms.Storage;

/// <summary>No storage configured: a file is refused with the reason, so the page says so rather than losing it.</summary>
public sealed class NullFormEvidenceStore : IFormEvidenceStore
{
    private const string Reason = "Form storage isn't configured (FormStorage:ConnectionString / DrawingsStorage:ConnectionString).";

    public bool IsConfigured => false;

    public Task SaveAsync(FormEvidenceStore store, string blobRef, string contentType, byte[] bytes, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException(Reason));

    public Task<StoredBlob?> OpenAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<StoredBlob?>(null);

    public Task<bool> DeleteAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken) => Task.FromResult(false);
}
