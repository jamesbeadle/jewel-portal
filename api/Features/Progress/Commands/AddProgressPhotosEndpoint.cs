using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// POST /api/progress-updates/{progressUpdateId}/photos — multipart/form-data upload of up to
/// <see cref="ProgressPhotoLimits.MaxImagesPerBatch"/> image files (JPEG, PNG, HEIC), appended to
/// an existing progress update in the order posted. Answers a <see cref="ProgressPhotoBatchResult"/>:
/// the update as it now stands and an outcome per image — stored, duplicate of one already held,
/// or failed on its own.
/// </summary>
public sealed class AddProgressPhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly ProgressPhotoIntake intake;
    private readonly AddProgressPhotosAuthorisation authorisation;
    private readonly AddProgressPhotosValidation validation;
    private readonly ICommandHandler<AddProgressPhotos, ProgressUpdate> handler;

    public AddProgressPhotosEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        ProgressPhotoIntake intake,
        AddProgressPhotosAuthorisation authorisation,
        AddProgressPhotosValidation validation,
        ICommandHandler<AddProgressPhotos, ProgressUpdate> handler)
    {
        this.users = users;
        this.context = context;
        this.intake = intake;
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
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var read = await ProgressPhotoFormReader.ReadAsync(form, cancellationToken);
        if (read.Refusal is not null) return new BadRequestObjectResult(read.Refusal);

        var update = await context.ProgressUpdates.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProgressUpdateId == progressUpdateId, cancellationToken);
        if (update is null) return new NotFoundObjectResult($"Progress update {progressUpdateId} not found.");

        var result = await ProgressPhotoBatches.AddAsync(
            intake, validation, handler, update.ProjectId, progressUpdateId, signedInUser.Email, read.Images, cancellationToken);
        return result.Failure is not null
            ? new BadRequestObjectResult(result.Failure)
            : new OkObjectResult(result.Batch);
    }
}
