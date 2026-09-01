using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;

/// <summary>
/// POST /api/tender-enquiries/{id}/attachments — multipart/form-data, one or more files. Files land
/// in the enquiry's private container and rows in its register; the response is the refreshed list.
/// </summary>
public sealed class TenderEnquiryAttachmentUploadEndpoint
{
    // Scanned questionnaires and drawing PDFs are a few MB; same ceiling as bid-package attachments.
    private const long MaxAttachmentBytes = 64L * 1024 * 1024;

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly TenderEnquiryAttachmentWriter writer;
    private readonly IQueryHandler<ListTenderEnquiryAttachments, IReadOnlyList<TenderEnquiryAttachment>> list;

    public TenderEnquiryAttachmentUploadEndpoint(
        SignedInUserResolver users, JpmsContext context, TenderEnquiryAttachmentWriter writer,
        IQueryHandler<ListTenderEnquiryAttachments, IReadOnlyList<TenderEnquiryAttachment>> list)
    {
        this.users = users;
        this.context = context;
        this.writer = writer;
        this.list = list;
    }

    [Function(nameof(UploadTenderEnquiryAttachments))]
    public async Task<IActionResult> UploadTenderEnquiryAttachments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tender-enquiries/{tenderEnquiryId}/attachments")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TenderEnquiryRoles.Managers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var files = form.Files.Where(file => file.Length > 0).ToList();
        if (files.Count == 0) return new BadRequestObjectResult("A non-empty file is required.");
        if (files.Any(file => file.Length > MaxAttachmentBytes))
            return new BadRequestObjectResult("One of those files is too large — attachments are limited to 64 MB each.");

        var enquiry = await context.TenderEnquiries.AsNoTracking()
            .FirstOrDefaultAsync(row => row.TenderEnquiryId == tenderEnquiryId, cancellationToken);
        if (enquiry is null) return new NotFoundObjectResult($"Tender enquiry {tenderEnquiryId} not found.");

        foreach (var file in files)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                await writer.StoreAsync(
                    enquiry, file.FileName, file.ContentType, file.Length, stream,
                    TenderEnquiryAttachmentSource.Upload, signedInUser.Email, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Files already stored in this post keep their rows — a partial upload is more
                // useful than losing the ones that did land.
                await context.SaveChangesAsync(cancellationToken);
                return new ObjectResult($"Could not store {file.FileName}. ({ex.Message})")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
        }
        await context.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(await list.HandleAsync(new ListTenderEnquiryAttachments(tenderEnquiryId), cancellationToken));
    }
}
