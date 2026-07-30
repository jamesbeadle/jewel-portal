using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Directory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RemoveDirectoryUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RemoveDirectoryUserAuthorisation authorisation;
    private readonly RemoveDirectoryUserValidation validation;
    private readonly ICommandHandler<RemoveDirectoryUser, Acknowledgement> handler;

    public RemoveDirectoryUserEndpoint(
        SignedInUserResolver users,
        RemoveDirectoryUserAuthorisation authorisation,
        RemoveDirectoryUserValidation validation,
        ICommandHandler<RemoveDirectoryUser, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RemoveDirectoryUser))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "directory/{email}")] HttpRequest request,
        string email)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        // RevokedBy is stamped here from the resolved caller — never trusted from the client.
        var command = new RemoveDirectoryUser(email, signedInUser.Email);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var acknowledgement = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(acknowledgement);
        }
        catch (InvalidOperationException ex)
        {
            // e.g. revoking the last active administrator.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
