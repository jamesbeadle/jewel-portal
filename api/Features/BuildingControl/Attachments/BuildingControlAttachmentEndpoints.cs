using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.BuildingControl.Attachments;

/// <summary>
/// The files' HTTP surface: multipart uploads onto the case or an inspection (?kind= names the
/// file's kind, defaulting to inference: image → Photo, PDF → Site inspection report), a proxied
/// download, re-kind, remove, and the copy-off-the-email route. Reads open to the whole internal
/// team; writes to the roles that run building control.
/// </summary>
public sealed class BuildingControlAttachmentEndpoints
{
    // Site photos and scanned reports are a few MB; same ceiling as tender-enquiry attachments.
    private const long MaxAttachmentBytes = 64L * 1024 * 1024;

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IBuildingControlAttachmentStore blobStore;
    private readonly BuildingControlAttachmentWriter writer;
    private readonly SetBuildingControlAttachmentKindAuthorisation setKindAuthorisation;
    private readonly ICommandHandler<SetBuildingControlAttachmentKind, BuildingControlAttachment> setKind;
    private readonly RemoveBuildingControlAttachmentAuthorisation removeAuthorisation;
    private readonly ICommandHandler<RemoveBuildingControlAttachment, Acknowledgement> remove;
    private readonly CopyEmailAttachmentsToBuildingControlInspectionAuthorisation copyAuthorisation;
    private readonly ICommandHandler<CopyEmailAttachmentsToBuildingControlInspection, IReadOnlyList<BuildingControlAttachment>> copy;

    public BuildingControlAttachmentEndpoints(
        SignedInUserResolver users, JpmsContext context,
        IBuildingControlAttachmentStore blobStore, BuildingControlAttachmentWriter writer,
        SetBuildingControlAttachmentKindAuthorisation setKindAuthorisation,
        ICommandHandler<SetBuildingControlAttachmentKind, BuildingControlAttachment> setKind,
        RemoveBuildingControlAttachmentAuthorisation removeAuthorisation,
        ICommandHandler<RemoveBuildingControlAttachment, Acknowledgement> remove,
        CopyEmailAttachmentsToBuildingControlInspectionAuthorisation copyAuthorisation,
        ICommandHandler<CopyEmailAttachmentsToBuildingControlInspection, IReadOnlyList<BuildingControlAttachment>> copy)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.writer = writer;
        this.setKindAuthorisation = setKindAuthorisation;
        this.setKind = setKind;
        this.removeAuthorisation = removeAuthorisation;
        this.remove = remove;
        this.copyAuthorisation = copyAuthorisation;
        this.copy = copy;
    }

    [Function(nameof(UploadBuildingControlCaseAttachments))]
    public async Task<IActionResult> UploadBuildingControlCaseAttachments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "building-control/cases/{caseId}/attachments")] HttpRequest request,
        string caseId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var buildingControlCase = await context.BuildingControlCases.AsNoTracking()
            .FirstOrDefaultAsync(row => row.BuildingControlCaseId == caseId, cancellationToken);
        if (buildingControlCase is null) return new NotFoundObjectResult($"Building control case {caseId} not found.");
        return await UploadAsync(request, buildingControlCase.ProjectId, caseId, inspectionId: null, cancellationToken);
    }

    [Function(nameof(UploadBuildingControlInspectionAttachments))]
    public async Task<IActionResult> UploadBuildingControlInspectionAttachments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "building-control/inspections/{inspectionId}/attachments")] HttpRequest request,
        string inspectionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var inspection = await context.BuildingControlInspections.AsNoTracking()
            .FirstOrDefaultAsync(row => row.BuildingControlInspectionId == inspectionId, cancellationToken);
        if (inspection is null) return new NotFoundObjectResult($"Inspection {inspectionId} not found.");
        return await UploadAsync(request, inspection.ProjectId, caseId: null, inspectionId, cancellationToken);
    }

    private async Task<IActionResult> UploadAsync(
        HttpRequest request, string projectId, string? caseId, string? inspectionId, CancellationToken cancellationToken)
    {
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!BuildingControlRoles.Managers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var files = form.Files.Where(file => file.Length > 0).ToList();
        if (files.Count == 0) return new BadRequestObjectResult("A non-empty file is required.");
        if (files.Any(file => file.Length > MaxAttachmentBytes))
            return new BadRequestObjectResult("One of those files is too large — attachments are limited to 64 MB each.");

        // ?kind= names what the files ARE; absent, each file's kind is inferred from its type.
        BuildingControlAttachmentKind? requestedKind = null;
        if (request.Query.TryGetValue("kind", out var kindValue)
            && int.TryParse(kindValue, out var kindNumber)
            && Enum.IsDefined(typeof(BuildingControlAttachmentKind), kindNumber))
        {
            requestedKind = (BuildingControlAttachmentKind)kindNumber;
        }

        foreach (var file in files)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var kind = requestedKind ?? BuildingControlRules.InferKind(file.ContentType, file.FileName);
                await writer.StoreAsync(
                    projectId, caseId, inspectionId, file.FileName, file.ContentType, file.Length, stream,
                    kind, BuildingControlAttachmentSource.Upload, signedInUser.Email, cancellationToken);
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
        return new OkObjectResult(await ListAsync(projectId, cancellationToken));
    }

    /// <summary>Streams a stored file. ?inline=1 renders it in place (the photo grid's
    /// thumbnails); otherwise it downloads.</summary>
    [Function(nameof(DownloadBuildingControlAttachmentFile))]
    public async Task<IActionResult> DownloadBuildingControlAttachmentFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "building-control/attachments/{attachmentId}/file")] HttpRequest request,
        string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!BuildingControlRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var entity = await context.BuildingControlAttachments.AsNoTracking().FirstOrDefaultAsync(
            row => row.BuildingControlAttachmentId == attachmentId, cancellationToken);
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

    [Function(nameof(SetBuildingControlAttachmentKind))]
    public async Task<IActionResult> SetKind(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "building-control/attachments/{attachmentId}/kind")] HttpRequest request,
        string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetBuildingControlAttachmentKind>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A kind body is required.");
        var command = posted with { BuildingControlAttachmentId = attachmentId };

        if (!setKindAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await setKind.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(RemoveBuildingControlAttachment))]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "building-control/attachments/{attachmentId}")] HttpRequest request,
        string attachmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveBuildingControlAttachment(attachmentId);
        if (!removeAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await remove.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(CopyEmailAttachmentsToBuildingControlInspection))]
    public async Task<IActionResult> CopyFromEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "building-control/inspections/{inspectionId}/copy-email-attachments")] HttpRequest request,
        string inspectionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CopyEmailAttachmentsToBuildingControlInspection>(cancellationToken);
        if (posted is null || string.IsNullOrWhiteSpace(posted.MessageId))
            return new BadRequestObjectResult("messageId is required.");
        var command = posted with { BuildingControlInspectionId = inspectionId, AddedByEmail = signedInUser.Email };

        if (!copyAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        try
        {
            return new OkObjectResult(await copy.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    private async Task<IReadOnlyList<BuildingControlAttachment>> ListAsync(string projectId, CancellationToken cancellationToken)
    {
        var rows = await context.BuildingControlAttachments.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}
