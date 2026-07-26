using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Attachments;

/// <summary>
/// Attachments on a request: linked drawing revisions and uploaded site photos. Reads open to the
/// whole internal team plus the architect (they are looking at the same RFI); writes are the roles
/// that raise and work requests — including the site manager and foreman, because taking the photo
/// on site IS the point of the feature.
/// </summary>
public sealed class RequestAttachmentEndpoints
{
    // Effectively "whatever the Functions host will accept" — phone photos are a few MB.
    private const long MaxAttachmentBytes = 64L * 1024 * 1024;

    private static readonly RoleSet AllowedToRead = JpmsRoleSets.InternalAndArchitect;
    private static readonly RoleSet AllowedToAttach = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.Foreman,
        JpmsRoles.Architect);

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IRequestAttachmentStore blobStore;
    private readonly Audit.AuditActor auditActor;
    private readonly IQueryHandler<ListRequestAttachments, IReadOnlyList<RequestAttachment>> list;
    private readonly ICommandHandler<AttachDrawingsToRequest, IReadOnlyList<RequestAttachment>> attachDrawings;
    private readonly ICommandHandler<RemoveRequestAttachment, IReadOnlyList<RequestAttachment>> remove;

    public RequestAttachmentEndpoints(
        SignedInUserResolver users,
        JpmsContext context,
        IRequestAttachmentStore blobStore,
        Audit.AuditActor auditActor,
        IQueryHandler<ListRequestAttachments, IReadOnlyList<RequestAttachment>> list,
        ICommandHandler<AttachDrawingsToRequest, IReadOnlyList<RequestAttachment>> attachDrawings,
        ICommandHandler<RemoveRequestAttachment, IReadOnlyList<RequestAttachment>> remove)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.auditActor = auditActor;
        this.list = list;
        this.attachDrawings = attachDrawings;
        this.remove = remove;
    }

    [Function(nameof(ListRequestAttachments))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/attachments")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await list.HandleAsync(new ListRequestAttachments(requestId), cancellationToken));
    }

    [Function(nameof(AttachDrawingsToRequest))]
    public async Task<IActionResult> AttachDrawings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/attachments/drawings")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToAttach.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        auditActor.Email = signedInUser.Email;

        AttachDrawingsToRequest? body = null;
        try { body = await request.ReadFromJsonAsync<AttachDrawingsToRequest>(cancellationToken); }
        catch { }
        if (body is null || body.DrawingRevisionIds is null || body.DrawingRevisionIds.Count == 0)
            return new BadRequestObjectResult("Pick at least one drawing to attach.");

        try
        {
            return new OkObjectResult(await attachDrawings.HandleAsync(
                body with { RequestId = requestId }, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    /// <summary>
    /// POST /api/requests/{requestId}/attachments — multipart/form-data, one or more files.
    /// Optional <c>caption</c> applies to every file in the post. Built for a phone: the picker on
    /// the client uses capture=environment, so "attach images" is the camera.
    /// </summary>
    [Function(nameof(UploadRequestAttachments))]
    public async Task<IActionResult> UploadRequestAttachments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/attachments")] HttpRequest request,
        string requestId)
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

        var requestRow = await context.Requests
            .FirstOrDefaultAsync(row => row.RequestId == requestId, cancellationToken);
        if (requestRow is null) return new NotFoundObjectResult($"Request {requestId} not found.");

        var caption = form["caption"].ToString().Trim();
        if (caption.Length > 512) caption = caption[..512];

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
                    requestRow.ProjectId, requestId, attachmentId, fileName, contentType, stream, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Files already stored in this post keep their rows — a partial upload is more
                // useful to someone on site than losing the three photos that did land.
                await context.SaveChangesAsync(cancellationToken);
                return new ObjectResult($"Could not store {fileName}. ({ex.Message})")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }

            context.RequestAttachments.Add(new RequestAttachmentEntity
            {
                RequestAttachmentId = attachmentId,
                RequestId = requestId,
                ProjectId = requestRow.ProjectId,
                Kind = (int)RequestAttachmentKind.File,
                FileName = fileName,
                ContentType = contentType,
                FileSizeBytes = file.Length,
                BlobRef = blobRef,
                Caption = string.IsNullOrWhiteSpace(caption) ? null : caption,
                AddedAt = now,
                AddedByEmail = signedInUser.Email
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(await list.HandleAsync(new ListRequestAttachments(requestId), cancellationToken));
    }

    [Function(nameof(RemoveRequestAttachment))]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "requests/{requestId}/attachments/{attachmentId}")] HttpRequest request,
        string requestId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToAttach.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await remove.HandleAsync(
            new RemoveRequestAttachment(requestId, attachmentId), cancellationToken));
    }

    /// <summary>
    /// GET /api/requests/{requestId}/attachments/{attachmentId}/file — streams an uploaded file.
    /// ?inline=1 renders it in place (how the photo thumbnails are drawn); otherwise it downloads.
    /// Drawing links have no file of their own — they are read through the drawings endpoint.
    /// </summary>
    [Function(nameof(DownloadRequestAttachmentFile))]
    public async Task<IActionResult> DownloadRequestAttachmentFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/attachments/{attachmentId}/file")] HttpRequest request,
        string requestId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var entity = await context.RequestAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.RequestAttachmentId == attachmentId && row.RequestId == requestId,
                cancellationToken);
        if (entity is null || string.IsNullOrWhiteSpace(entity.BlobRef))
            return new NotFoundObjectResult("No file is stored for this attachment.");

        var blob = await blobStore.OpenAsync(entity.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        var result = new FileStreamResult(blob.Content, entity.ContentType ?? blob.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline)
            result.FileDownloadName = string.IsNullOrWhiteSpace(entity.FileName) ? attachmentId : entity.FileName;
        return result;
    }
}
