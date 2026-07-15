using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// POST /api/projects/{projectId}/progress-updates — multipart/form-data upload.
/// Form fields: <c>title</c>, optional <c>description</c>, optional <c>workDate</c> (ISO 8601),
/// plus one or more image files. Streams every file to blob storage, then records the update and
/// its photo rows in one save.
/// </summary>
public sealed class CreateProgressUpdateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;
    private readonly CreateProgressUpdateAuthorisation authorisation;
    private readonly CreateProgressUpdateValidation validation;
    private readonly ICommandHandler<CreateProgressUpdate, ProgressUpdate> handler;

    public CreateProgressUpdateEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProgressPhotoStore photoStore,
        CreateProgressUpdateAuthorisation authorisation,
        CreateProgressUpdateValidation validation,
        ICommandHandler<CreateProgressUpdate, ProgressUpdate> handler)
    {
        this.users = users;
        this.context = context;
        this.photoStore = photoStore;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateProgressUpdate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/progress-updates")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new ForbidResult();

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        if (form.Files.Count == 0) return new BadRequestObjectResult("At least one photo is required.");

        var projectExists = await context.Projects
            .AnyAsync(row => row.ProjectId == projectId, cancellationToken);
        if (!projectExists) return new NotFoundObjectResult($"Project {projectId} not found.");

        var title = form["title"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(title)) return new BadRequestObjectResult("A title is required.");
        var description = form["description"].ToString().Trim();
        DateTimeOffset? workDate = DateTimeOffset.TryParse(form["workDate"], out var parsed) ? parsed : null;

        var updateId = ProgressIdentifierFactory.NextProgressUpdateId();

        var photos = new List<NewProgressPhoto>();
        try
        {
            var sortOrder = 0;
            foreach (var file in form.Files)
            {
                if (file.Length == 0) continue;
                var photoId = ProgressIdentifierFactory.NextProgressPhotoId();
                var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "photo" : file.FileName;
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

                await using var stream = file.OpenReadStream();
                var blobRef = await photoStore.UploadAsync(
                    projectId, updateId, photoId, fileName, contentType, stream, cancellationToken);

                photos.Add(new NewProgressPhoto(photoId, fileName, blobRef, contentType, file.Length, sortOrder++));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Storage misconfigured/unreachable — report it clearly rather than letting the request
            // hang or surface as an opaque 500. Nothing is recorded, so no orphan rows.
            return new ObjectResult($"Could not store the photos — check the progress photo storage configuration. ({ex.Message})")
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }

        var command = new CreateProgressUpdate(
            updateId, projectId, title, description, workDate, signedInUser.Email, photos);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var update = await handler.HandleAsync(command, cancellationToken);
        return new OkObjectResult(update);
    }
}
