using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;

/// <summary>List, remove and stream the files kept on an enquiry. Reads open to the whole internal
/// team; removal to the roles that run bids. Uploads live in their own endpoint (multipart).</summary>
public sealed class TenderEnquiryAttachmentReadEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly ITenderEnquiryAttachmentStore blobStore;
    private readonly IQueryHandler<ListTenderEnquiryAttachments, IReadOnlyList<TenderEnquiryAttachment>> list;
    private readonly ICommandHandler<RemoveTenderEnquiryAttachment, IReadOnlyList<TenderEnquiryAttachment>> remove;

    public TenderEnquiryAttachmentReadEndpoints(
        SignedInUserResolver users, JpmsContext context, ITenderEnquiryAttachmentStore blobStore,
        IQueryHandler<ListTenderEnquiryAttachments, IReadOnlyList<TenderEnquiryAttachment>> list,
        ICommandHandler<RemoveTenderEnquiryAttachment, IReadOnlyList<TenderEnquiryAttachment>> remove)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.list = list;
        this.remove = remove;
    }

    [Function(nameof(ListTenderEnquiryAttachments))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tender-enquiries/{tenderEnquiryId}/attachments")] HttpRequest request,
        string tenderEnquiryId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TenderEnquiryRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await list.HandleAsync(new ListTenderEnquiryAttachments(tenderEnquiryId), cancellationToken));
    }

    [Function(nameof(RemoveTenderEnquiryAttachment))]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tender-enquiries/{tenderEnquiryId}/attachments/{attachmentId}")] HttpRequest request,
        string tenderEnquiryId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TenderEnquiryRoles.Managers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await remove.HandleAsync(
            new RemoveTenderEnquiryAttachment(tenderEnquiryId, attachmentId), cancellationToken));
    }

    /// <summary>Streams a stored file. ?inline=1 renders it in place; otherwise it downloads.</summary>
    [Function(nameof(DownloadTenderEnquiryAttachmentFile))]
    public async Task<IActionResult> DownloadTenderEnquiryAttachmentFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tender-enquiries/{tenderEnquiryId}/attachments/{attachmentId}/file")] HttpRequest request,
        string tenderEnquiryId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TenderEnquiryRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var entity = await context.TenderEnquiryAttachments.AsNoTracking().FirstOrDefaultAsync(
            row => row.TenderEnquiryAttachmentId == attachmentId && row.TenderEnquiryId == tenderEnquiryId, cancellationToken);
        if (entity is null || string.IsNullOrWhiteSpace(entity.BlobRef))
            return new NotFoundObjectResult("No file is stored for this attachment.");

        var blob = await blobStore.OpenAsync(entity.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var isInline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));
        var contentType = string.IsNullOrWhiteSpace(entity.ContentType) ? blob.ContentType : entity.ContentType;
        var result = new FileStreamResult(blob.Content, contentType) { EnableRangeProcessing = true };
        if (!isInline) result.FileDownloadName = string.IsNullOrWhiteSpace(entity.FileName) ? attachmentId : entity.FileName;
        return result;
    }
}
