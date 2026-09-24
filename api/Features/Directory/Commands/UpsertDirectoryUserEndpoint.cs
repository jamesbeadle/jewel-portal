using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class UpsertDirectoryUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpsertDirectoryUserAuthorisation authorisation;
    private readonly UpsertDirectoryUserValidation validation;
    private readonly ICommandHandler<UpsertDirectoryUser, DirectoryUser> handler;
    private readonly ScopedRoleGrants scopedRoles;

    public UpsertDirectoryUserEndpoint(
        SignedInUserResolver users,
        UpsertDirectoryUserAuthorisation authorisation,
        UpsertDirectoryUserValidation validation,
        ICommandHandler<UpsertDirectoryUser, DirectoryUser> handler,
        ScopedRoleGrants scopedRoles)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.scopedRoles = scopedRoles;
    }

    [Function(nameof(UpsertDirectoryUser))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "directory")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpsertDirectoryUser>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var refusal = await scopedRoles.RefusalAsync(command.Email, command.Roles, request.HttpContext.RequestAborted);
        if (refusal is not null) return new BadRequestObjectResult(new[] { refusal });

        var directoryUser = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(directoryUser);
    }
}
