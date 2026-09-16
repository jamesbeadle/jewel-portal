using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>
/// POST /api/projects/{projectId}/progress-updates/note — JSON. Creates a progress update from its
/// words alone; photographs follow through POST progress-updates/{id}/photos. The multipart form
/// at POST /api/projects/{projectId}/progress-updates (the Progress page's own) is
/// <see cref="CreateProgressUpdateWithPhotosEndpoint"/>.
/// </summary>
public sealed class CreateProgressUpdateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateProgressUpdateAuthorisation authorisation;
    private readonly CreateProgressUpdateValidation validation;
    private readonly ICommandHandler<CreateProgressUpdate, ProgressUpdate> handler;

    public CreateProgressUpdateEndpoint(
        SignedInUserResolver users,
        CreateProgressUpdateAuthorisation authorisation,
        CreateProgressUpdateValidation validation,
        ICommandHandler<CreateProgressUpdate, ProgressUpdate> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateProgressUpdate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/progress-updates/note")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        var posted = await request.ReadFromJsonAsync<CreateProgressUpdate>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A progress update body is required.");

        var command = posted with { ProjectId = projectId, CreatedByEmail = signedInUser.Email };
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException guard)
        {
            return new NotFoundObjectResult(guard.Message);
        }
    }
}
