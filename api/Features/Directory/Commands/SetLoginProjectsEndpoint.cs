using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class SetLoginProjectsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetLoginProjectsAuthorisation authorisation;
    private readonly SetLoginProjectsValidation validation;
    private readonly ICommandHandler<SetLoginProjects, Acknowledgement> handler;

    public SetLoginProjectsEndpoint(
        SignedInUserResolver users,
        SetLoginProjectsAuthorisation authorisation,
        SetLoginProjectsValidation validation,
        ICommandHandler<SetLoginProjects, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetLoginProjects))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "directory/projects")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetLoginProjects>();
        if (posted is null) return new BadRequestResult();

        // GrantedByEmail is stamped here from the resolved caller — never trusted from the client.
        var command = posted with { GrantedByEmail = signedInUser.Email };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new[] { ex.Message });
        }
    }
}
