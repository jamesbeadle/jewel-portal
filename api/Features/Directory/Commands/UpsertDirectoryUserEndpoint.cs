using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class UpsertDirectoryUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpsertDirectoryUserAuthorisation authorisation;
    private readonly UpsertDirectoryUserValidation validation;
    private readonly ICommandHandler<UpsertDirectoryUser, DirectoryUser> handler;

    public UpsertDirectoryUserEndpoint(
        SignedInUserResolver users,
        UpsertDirectoryUserAuthorisation authorisation,
        UpsertDirectoryUserValidation validation,
        ICommandHandler<UpsertDirectoryUser, DirectoryUser> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpsertDirectoryUser))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "directory")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpsertDirectoryUser>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var directoryUser = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(directoryUser);
    }
}
