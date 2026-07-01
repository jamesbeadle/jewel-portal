using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// POST /api/drawings/{drawingId}/revisions — multipart/form-data upload.
/// Form fields: <c>file</c> (the drawing file), <c>revisionLabel</c>, optional <c>issuedByEmail</c>.
/// Streams the file to blob storage, then records an Unapproved revision. No sibling is superseded.
/// </summary>
public sealed class UploadDrawingRevisionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore blobStore;
    private readonly UploadDrawingRevisionAuthorisation authorisation;
    private readonly UploadDrawingRevisionValidation validation;
    private readonly ICommandHandler<UploadDrawingRevision, DrawingRevision> handler;

    public UploadDrawingRevisionEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IDrawingBlobStore blobStore,
        UploadDrawingRevisionAuthorisation authorisation,
        UploadDrawingRevisionValidation validation,
        ICommandHandler<UploadDrawingRevision, DrawingRevision> handler)
    {
        this.users = users;
        this.context = context;
        this.blobStore = blobStore;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UploadDrawingRevision))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "drawings/{drawingId}/revisions")] HttpRequest request,
        string drawingId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new ForbidResult();

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0) return new BadRequestObjectResult("A non-empty file is required.");

        var drawing = await context.Drawings
            .FirstOrDefaultAsync(row => row.DrawingId == drawingId, cancellationToken);
        if (drawing is null) return new NotFoundObjectResult($"Drawing {drawingId} not found.");

        var revisionLabel = form["revisionLabel"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(revisionLabel)) revisionLabel = "?";
        var issuedByEmail = form["issuedByEmail"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(issuedByEmail)) issuedByEmail = signedInUser.Email;

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "drawing" : file.FileName;
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        var revisionId = DrawingIdentifierFactory.NextDrawingRevisionId();

        string blobRef;
        await using (var stream = file.OpenReadStream())
        {
            blobRef = await blobStore.UploadAsync(
                drawing.ProjectId, drawingId, revisionId, fileName, contentType, stream, cancellationToken);
        }

        var command = new UploadDrawingRevision(
            drawingId, revisionId, revisionLabel, fileName, issuedByEmail, blobRef, contentType, file.Length);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var revision = await handler.HandleAsync(command, cancellationToken);
        return new OkObjectResult(revision);
    }
}
