using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Forms.Storage;

/// <summary>
/// Where a form's files live. Three private containers, never one: right-to-work evidence and
/// payroll starter evidence each have their own, so access can be limited to the people who need
/// it and the records can be found and destroyed on their retention date without sifting through
/// everything else (api/forms-intake.js RESTRICTED). Bytes only ever leave through the API.
/// </summary>
public interface IFormEvidenceStore
{
    bool IsConfigured { get; }
    Task SaveAsync(FormEvidenceStore store, string blobRef, string contentType, byte[] bytes, CancellationToken cancellationToken);
    Task<StoredBlob?> OpenAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken);
    /// <summary>Deletes a file, answering whether it was there — a destruction that finds nothing is reported, not assumed.</summary>
    Task<bool> DeleteAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken);
}
