using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

/// <summary>
/// Attachments kept on a work order for record keeping — the quote the order was raised against,
/// a signed copy, a photo of the scope. Reads open to the whole internal team (the same set that
/// reads the procurement views); writes are the roles that raise and edit work orders, mirroring
/// CreateManualWorkOrderAuthorisation. Nothing here ever reaches the supplier: the purchase-order
/// email and printed PO ignore attachments entirely.
/// </summary>
public sealed class WorkOrderAttachmentEndpoints
{
    // Same practical ceiling as request attachments — effectively "whatever the Functions host
    // will accept"; scanned quotes and photos are a few MB.
    private const long MaxAttachmentBytes = 64L * 1024 * 1024;

    private static readonly RoleSet AllowedToRead = JpmsRoleSets.AllInternal;
    private static readonly RoleSet AllowedToAttach = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator);

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IWorkOrderAttachmentStore blobStore;
    private readonly IQueryHandler<ListWorkOrderAttachments, IReadOnlyList<WorkOrderAttachment>> list;
    private readonly ICommandHandler<RemoveWorkOrderAttachment, IReadOnlyList<WorkOrderAttachment>> remove;
    private readonly ICommandHandler<AttachChatFilesToWorkOrder, IReadOnlyList<WorkOrderAttachment>> attachFromChat;

    public WorkOrderAttachmentEndpoints(
        SignedInUserResolver users,
        JpmsContext context,
        IWorkOrderAttachmentStore blobStore,
        IQueryHandler<ListWorkOrderAttachments, IReadOnlyList<WorkOrderAttachment>> list,
        ICommandHandler<RemoveWorkOrderAttachment, IReadOnlyList<WorkOrderAttachment>> remove,
        ICommandHandler<AttachChatFilesToWorkOrder, IReadOnlyList<WorkOrderAttachment>> attachFromChat)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.list = list;
        this.remove = remove;
        this.attachFromChat = attachFromChat;
    }

    [Function(nameof(ListWorkOrderAttachments))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "work-orders/{workOrderId}/attachments")] HttpRequest request,
        string workOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await list.HandleAsync(new ListWorkOrderAttachments(workOrderId), cancellationToken));
    }

    /// <summary>
    /// POST /api/work-orders/{workOrderId}/attachments — multipart/form-data, one or more files.
    /// Files land in the order's private container and rows in its attachment register; the
    /// response is the refreshed list. Record keeping only — nothing is emailed anywhere.
    /// </summary>
    [Function(nameof(UploadWorkOrderAttachments))]
    public async Task<IActionResult> UploadWorkOrderAttachments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "work-orders/{workOrderId}/attachments")] HttpRequest request,
        string workOrderId)
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

        var order = await context.WorkOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.WorkOrderId == workOrderId, cancellationToken);
        if (order is null) return new NotFoundObjectResult($"Work order {workOrderId} not found.");

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
                    order.ProjectId, workOrderId, attachmentId, fileName, contentType, stream, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Files already stored in this post keep their rows — a partial upload is more
                // useful than losing the ones that did land. Same trade as request attachments.
                await context.SaveChangesAsync(cancellationToken);
                return new ObjectResult($"Could not store {fileName}. ({ex.Message})")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }

            context.WorkOrderAttachments.Add(new WorkOrderAttachmentEntity
            {
                WorkOrderAttachmentId = attachmentId,
                WorkOrderId = workOrderId,
                ProjectId = order.ProjectId,
                FileName = fileName,
                ContentType = contentType,
                FileSizeBytes = file.Length,
                BlobRef = blobRef,
                Source = (int)WorkOrderAttachmentSource.Upload,
                AddedAt = now,
                AddedByEmail = signedInUser.Email
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(await list.HandleAsync(new ListWorkOrderAttachments(workOrderId), cancellationToken));
    }

    /// <summary>
    /// POST /api/work-orders/{workOrderId}/attachments/from-chat — copies files the caller
    /// attached to an assistant conversation onto the order's register (the quote the order was
    /// drafted from). JSON body, no bytes: the copy happens server-side, ai-attachments store →
    /// this order's container. Same writer roles as an upload; the handler additionally checks
    /// the conversation belongs to the signed-in user.
    /// </summary>
    [Function(nameof(AttachChatFilesToWorkOrder))]
    public async Task<IActionResult> AttachFromChat(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "work-orders/{workOrderId}/attachments/from-chat")] HttpRequest request,
        string workOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToAttach.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        AttachChatFilesToWorkOrder? body;
        try { body = await request.ReadFromJsonAsync<AttachChatFilesToWorkOrder>(cancellationToken); }
        catch { body = null; }
        if (body is null || string.IsNullOrWhiteSpace(body.ConversationId) || body.AttachmentIds is null)
            return new BadRequestObjectResult("A conversation id and the files to copy are required.");

        var command = body with { WorkOrderId = workOrderId, RequestedByEmail = signedInUser.Email };
        try
        {
            return new OkObjectResult(await attachFromChat.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's own refusals (order missing, chat not the caller's, a file's bytes
            // gone) are sentences worth showing verbatim in the dialog.
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(RemoveWorkOrderAttachment))]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "work-orders/{workOrderId}/attachments/{attachmentId}")] HttpRequest request,
        string workOrderId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToAttach.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await remove.HandleAsync(
            new RemoveWorkOrderAttachment(workOrderId, attachmentId), cancellationToken));
    }

    /// <summary>
    /// GET /api/work-orders/{workOrderId}/attachments/{attachmentId}/file — streams a stored file.
    /// ?inline=1 renders it in place (thumbnails, preview); otherwise it downloads.
    /// </summary>
    [Function(nameof(DownloadWorkOrderAttachmentFile))]
    public async Task<IActionResult> DownloadWorkOrderAttachmentFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "work-orders/{workOrderId}/attachments/{attachmentId}/file")] HttpRequest request,
        string workOrderId, string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var entity = await context.WorkOrderAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.WorkOrderAttachmentId == attachmentId && row.WorkOrderId == workOrderId,
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
