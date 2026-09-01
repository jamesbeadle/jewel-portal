using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RestoreDirectoryUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RestoreDirectoryUserAuthorisation authorisation;
    private readonly RestoreDirectoryUserValidation validation;
    private readonly ICommandHandler<RestoreDirectoryUser, Acknowledgement> handler;

    public RestoreDirectoryUserEndpoint(
        SignedInUserResolver users,
        RestoreDirectoryUserAuthorisation authorisation,
        RestoreDirectoryUserValidation validation,
        ICommandHandler<RestoreDirectoryUser, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RestoreDirectoryUser))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "directory/{email}/restore")] HttpRequest request,
        string email)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RestoreDirectoryUser(email);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // e.g. the record was permanently deleted between the list loading and the click.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
