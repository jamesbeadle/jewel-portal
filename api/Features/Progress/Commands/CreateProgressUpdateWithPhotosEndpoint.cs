using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// POST /api/projects/{projectId}/progress-updates — multipart/form-data upload, the Progress
/// page's own form. Form fields: <c>title</c>, optional <c>description</c>, optional
/// <c>workDate</c> (ISO 8601), the optional weather fields <see cref="ProgressWeatherForm"/> reads,
/// plus one or more image files (JPEG, PNG, HEIC — prepared and deduplicated by
/// <see cref="ProgressPhotoIntake"/>). Records the update and its photo rows in one save and
/// answers a <see cref="ProgressPhotoBatchResult"/>.
/// </summary>
public sealed class CreateProgressUpdateWithPhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly ProgressPhotoIntake intake;
    private readonly CreateProgressUpdateWithPhotosAuthorisation authorisation;
    private readonly CreateProgressUpdateWithPhotosValidation validation;
    private readonly ICommandHandler<CreateProgressUpdateWithPhotos, ProgressUpdate> handler;

    public CreateProgressUpdateWithPhotosEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        ProgressPhotoIntake intake,
        CreateProgressUpdateWithPhotosAuthorisation authorisation,
        CreateProgressUpdateWithPhotosValidation validation,
        ICommandHandler<CreateProgressUpdateWithPhotos, ProgressUpdate> handler)
    {
        this.users = users;
        this.context = context;
        this.intake = intake;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateProgressUpdateWithPhotos))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/progress-updates")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var read = await ProgressPhotoFormReader.ReadAsync(form, cancellationToken);
        if (read.Refusal is not null) return new BadRequestObjectResult(read.Refusal);

        var projectExists = await context.Projects.AnyAsync(row => row.ProjectId == projectId, cancellationToken);
        if (!projectExists) return new NotFoundObjectResult($"Project {projectId} not found.");

        var title = form["title"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(title)) return new BadRequestObjectResult("A title is required.");
        var description = form["description"].ToString().Trim();
        DateTimeOffset? workDate = DateTimeOffset.TryParse(form["workDate"], out var parsed) ? parsed : null;
        var weather = ProgressWeatherForm.Read(form);

        var updateId = ProgressIdentifierFactory.NextProgressUpdateId();
        var taken = await intake.TakeAsync(projectId, updateId, read.Images, cancellationToken);
        if (taken.Stored.Count == 0)
            return new BadRequestObjectResult(new { error = "None of the images could be stored.", outcomes = taken.Outcomes });

        var command = new CreateProgressUpdateWithPhotos(
            updateId, projectId, title, description, workDate, weather, signedInUser.Email, taken.Stored);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var update = await handler.HandleAsync(command, cancellationToken);
        return new OkObjectResult(new ProgressPhotoBatchResult(update, taken.Outcomes));
    }
}
