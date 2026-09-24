using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Registers.Policies;

/// <summary>
/// The PDF a policy revision is read from, kept in the form evidence store's general container under
/// policies/{revision}/ — the store the Policy sign-off form already reads from, so a person with a
/// link reads it without a portal login. A revision's file is fixed once anyone has signed it: what
/// they signed is what stays.
/// </summary>
internal static class PolicyFiles
{
    public const long LargestBytes = 25L * 1024 * 1024;
    private const string PdfType = "application/pdf";

    public static bool IsAPdf(string fileName, string contentType) =>
        string.Equals(contentType, PdfType, StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public static async Task<StoredBlob?> OpenAsync(IFormEvidenceStore store, PolicyDocumentEntity policy, CancellationToken cancellationToken)
    {
        var hasAFile = policy.FileBlobRef.Length > 0;
        return hasAFile ? await store.OpenAsync(FormEvidenceStore.General, policy.FileBlobRef, cancellationToken) : null;
    }

    public static async Task AttachAsync(
        IFormEvidenceStore store, PolicyDocumentEntity policy, string fileName, byte[] bytes, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(fileName);
        var blobRef = $"policies/{policy.PolicyDocumentId}/{Guid.NewGuid():N}.pdf";
        await store.SaveAsync(FormEvidenceStore.General, blobRef, PdfType, bytes, cancellationToken);
        policy.FileBlobRef = blobRef;
        policy.FileName = safeName.Length > 0 ? safeName : $"{policy.Title} rev {policy.Revision}.pdf";
    }
}
