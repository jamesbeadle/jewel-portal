using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

/// <summary>
/// Attachments kept on a bid package as tender documents — specification extracts, schedules of
/// finishes, survey photos: anything a tenderer needs that isn't a drawing in the register. Reads
/// open to the whole internal team; writes are the roles that create and scope packages, mirroring
/// CreateBidPackageAuthorisation. These ARE sent to subcontractors: the invite draft attaches them
/// alongside the linked drawings (oversized files travel as 7-day download links).
/// </summary>
public sealed class BidPackageAttachmentEndpoints
{
    // Same practical ceiling as work-order attachments — scanned specs and photos are a few MB.
    private const long MaxAttachmentBytes = 64L * 1024 * 1024;

    private static readonly RoleSet AllowedToRead = JpmsRoleSets.AllInternal;
    private static readonly RoleSet AllowedToAttach = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IBidPackageAttachmentStore blobStore;
    private readonly IQueryHandler<ListBidPackageAttachments, IReadOnlyList<BidPackageAttachment>> list;
    private readonly ICommandHandler<RemoveBidPackageAttachment, IReadOnlyList<BidPackageAttachment>> remove;

    public BidPackageAttachmentEndpoints(
        SignedInUserResolver users,
        JpmsContext context,
        IBidPackageAttachmentStore blobStore,
        IQueryHandler<ListBidPackageAttachments, IReadOnlyList<BidPackageAttachment>> list,
        ICommandHandler<RemoveBidPackageAttachment, IReadOnlyList<BidPackageAttachment>> remove)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.list = list;
        this.remove = remove;
    }

    [Function(nameof(ListBidPackageAttachments))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/attachments")] HttpRequest request,
        string bidPackageId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await list.HandleAsync(new ListBidPackageAttachments(bidPackageId), cancellationToken));
    }

    /// <summary>
    /// POST /api/bid-packages/{bidPackageId}/attachments — multipart/form-data, one or more files.
    /// Files land in the package's private container and rows in its attachment register; the
    /// response is the refreshed list. They travel with the invite when it is drafted.
    /// </summary>
    [Function(nameof(UploadBidPackageAttachments))]
    public async Task<IActionResult> UploadBidPackageAttachments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/attachments")] HttpRequest request,
        string bidPackageId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToAttach.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var files = form.Files.Where(file => file.Length > 0).ToList();
        if (files.Count == 0) return new BadRequestObjectResult("A non-empty file is required.");
        if (files.Any(file => file.Length > MaxAttachmentBytes))
            return new BadRequestObjectResult("One of those files is too large — attachments are limited to 64 MB each.");

        var package = await context.BidPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.BidPackageId == bidPackageId, cancellationToken);
        if (package is null) return new NotFoundObjectResult($"Bid package {bidPackageId} not found.");
        if (package.Status == (int)BidPackageStatus.Closed)
            return new BadRequestObjectResult("This bid package is closed — reopen it before adding tender documents.");

        var now = DateTimeOffset.UtcNow;
        foreach (var file in files)
        {
            var attachmentId = Guid.NewGuid().ToString("N");
            var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "attachment" : file.FileName;
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            string blobRef;
            try
            {
                await using var stream = file.OpenReadStream();
                blobRef = await blobStore.UploadAsync(
                    package.ProjectId, bidPackageId, attachmentId, fileName, contentType, stream, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Files already stored in this post keep their rows — a partial upload is more
                // useful than losing the ones that did land. Same trade as work-order attachments.
                await context.SaveChangesAsync(cancellationToken);
                return new ObjectResult($"Could not store {fileName}. ({ex.Message})")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }

            context.BidPackageAttachments.Add(new BidPackageAttachmentEntity
            {
                BidPackageAttachmentId = attachmentId,
                BidPackageId = bidPackageId,
                ProjectId = package.ProjectId,
                FileName = fileName,
                ContentType = contentType,
                FileSizeBytes = file.Length,
                BlobRef = blobRef,
                Source = (int)BidPackageAttachmentSource.Upload,
                AddedAt = now,
                AddedByEmail = signedInUser.Email
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(await list.HandleAsync(new ListBidPackageAttachments(bidPackageId), cancellationToken));
    }

    [Function(nameof(RemoveBidPackageAttachment))]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "bid-packages/{bidPackageId}/attachments/{attachmentId}")] HttpRequest request,
        string bidPackageId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToAttach.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await remove.HandleAsync(
            new RemoveBidPackageAttachment(bidPackageId, attachmentId), cancellationToken));
    }

    /// <summary>
    /// GET /api/bid-packages/{bidPackageId}/attachments/{attachmentId}/file — streams a stored
    /// file. ?inline=1 renders it in place (thumbnails, preview); otherwise it downloads.
    /// </summary>
    [Function(nameof(DownloadBidPackageAttachmentFile))]
    public async Task<IActionResult> DownloadBidPackageAttachmentFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/attachments/{attachmentId}/file")] HttpRequest request,
        string bidPackageId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var entity = await context.BidPackageAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.BidPackageAttachmentId == attachmentId && row.BidPackageId == bidPackageId,
                cancellationToken);
        if (entity is null || string.IsNullOrWhiteSpace(entity.BlobRef))
            return new NotFoundObjectResult("No file is stored for this attachment.");

        var blob = await blobStore.OpenAsync(entity.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        var result = new FileStreamResult(blob.Content, string.IsNullOrWhiteSpace(entity.ContentType) ? blob.ContentType : entity.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline)
            result.FileDownloadName = string.IsNullOrWhiteSpace(entity.FileName) ? attachmentId : entity.FileName;
        return result;
    }
}
