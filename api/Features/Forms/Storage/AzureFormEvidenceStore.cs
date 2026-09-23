using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Forms.Storage;

/// <summary>The four containers over the shared blob shell, one per store, keyed by the upload's own reference.</summary>
public sealed class AzureFormEvidenceStore : IFormEvidenceStore
{
    public const string GeneralContainer = "form-uploads";
    public const string RightToWorkContainer = "form-right-to-work";
    public const string PayrollStartersContainer = "form-payroll-starters";
    public const string HealthAndSafetyContainer = "form-health-and-safety";

    private readonly AzureBlobFileStore general;
    private readonly AzureBlobFileStore rightToWork;
    private readonly AzureBlobFileStore payrollStarters;
    private readonly AzureBlobFileStore healthAndSafety;

    public AzureFormEvidenceStore(string connectionString)
    {
        general = new AzureBlobFileStore(connectionString, GeneralContainer);
        rightToWork = new AzureBlobFileStore(connectionString, RightToWorkContainer);
        payrollStarters = new AzureBlobFileStore(connectionString, PayrollStartersContainer);
        healthAndSafety = new AzureBlobFileStore(connectionString, HealthAndSafetyContainer);
    }

    public bool IsConfigured => true;

    public async Task SaveAsync(FormEvidenceStore store, string blobRef, string contentType, byte[] bytes, CancellationToken cancellationToken)
    {
        using var content = new MemoryStream(bytes, writable: false);
        await ContainerFor(store).UploadAsync(blobRef, contentType, content, cancellationToken);
    }

    public Task<StoredBlob?> OpenAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken) =>
        ContainerFor(store).OpenAsync(blobRef, cancellationToken);

    public Task<bool> DeleteAsync(FormEvidenceStore store, string blobRef, CancellationToken cancellationToken) =>
        ContainerFor(store).DeleteAsync(blobRef, cancellationToken);

    private AzureBlobFileStore ContainerFor(FormEvidenceStore store) => store switch
    {
        FormEvidenceStore.RightToWork => rightToWork,
        FormEvidenceStore.PayrollStarters => payrollStarters,
        FormEvidenceStore.HealthAndSafety => healthAndSafety,
        _ => general
    };
}
