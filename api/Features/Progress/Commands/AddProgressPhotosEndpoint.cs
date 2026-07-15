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
/// POST /api/progress-updates/{progressUpdateId}/photos — multipart/form-data upload of one or
/// more image files, appended to an existing progress update.
/// </summary>
public sealed class AddProgressPhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;
    private readonly AddProgressPhotosAuthorisation authorisation;
    private readonly AddProgressPhotosValidation validation;
    private readonly ICommandHandler<AddProgressPhotos, ProgressUpdate> handler;

    public AddProgressPhotosEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProgressPhotoStore photoStore,
        AddProgressPhotosAuthorisation authorisation,
        AddProgressPhotosValidation validation,
        ICommandHandler<AddProgressPhotos, ProgressUpdate> handler)
    {
        this.users = users;
        this.context = context;
        this.photoStore = photoStore;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(AddProgressPhotos))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "progress-updates/{progressUpdateId}/photos")] HttpRequest request,
        string progressUpdateId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new ForbidResult();

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        if (form.Files.Count == 0) return new BadRequestObjectResult("At least one photo is required.");

        var update = await context.ProgressUpdates
            .FirstOrDefaultAsync(row => row.ProgressUpdateId == progressUpdateId, cancellationToken);
        if (update is null) return new NotFoundObjectResult($"Progress update {progressUpdateId} not found.");

        var photos = new List<NewProgressPhoto>();
        try
        {
            var position = 0;
            foreach (var file in form.Files)
            {
                if (file.Length == 0) continue;
                var photoId = ProgressIdentifierFactory.NextProgressPhotoId();
                var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "photo" : file.FileName;
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

                await using var stream = file.OpenReadStream();
                var blobRef = await photoStore.UploadAsync(
                    update.ProjectId, progressUpdateId, photoId, fileName, contentType, stream, cancellationToken);

                photos.Add(new NewProgressPhoto(photoId, fileName, blobRef, contentType, file.Length, position++));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ObjectResult($"Could not store the photos — check the progress photo storage configuration. ({ex.Message})")
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }

        var command = new AddProgressPhotos(progressUpdateId, signedInUser.Email, photos);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var result = await handler.HandleAsync(command, cancellationToken);
        return new OkObjectResult(result);
    }
}
