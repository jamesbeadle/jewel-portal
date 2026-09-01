using Jewel.JPMS.Api.Features.DocumentControl.Storage;

namespace Jewel.JPMS.Api.Features.DocumentControl.Queries;

/// <summary>
/// GET /api/finance/payment-certificates/{certificateId}/file — streams a certificate's stored
/// copy. Private container, proxied through the API; ?inline=1 for the in-app viewer.
/// </summary>
public sealed class DownloadPaymentCertificateFileEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IDocumentControlBlobStore blobStore;

    public DownloadPaymentCertificateFileEndpoint(
        SignedInUserResolver users, JpmsContext context, IDocumentControlBlobStore blobStore)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
    }

    [Function(nameof(DownloadPaymentCertificateFileEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "finance/payment-certificates/{certificateId}/file")] HttpRequest request,
        string certificateId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DocumentControlRoles.AllowedToReadPaymentCertificates.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(403);

        var certificate = await context.PaymentCertificates.AsNoTracking()
            .FirstOrDefaultAsync(row => row.PaymentCertificateId == certificateId, cancellationToken);
        if (certificate is null || string.IsNullOrWhiteSpace(certificate.BlobRef))
            return new NotFoundObjectResult("No file is stored for this certificate.");

        var blob = await blobStore.OpenAsync(certificate.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        var result = new FileStreamResult(blob.Content, string.IsNullOrWhiteSpace(certificate.ContentType) ? blob.ContentType : certificate.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline)
            result.FileDownloadName = string.IsNullOrWhiteSpace(certificate.FileName) ? certificateId : certificate.FileName;
        return result;
    }
}
