using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

/// <summary>POST /api/architects — create an architect practice.</summary>
public sealed class CreateArchitectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateArchitectAuthorisation authorisation;
    private readonly CreateArchitectValidation validation;
    private readonly ICommandHandler<CreateArchitect, Architect> handler;

    public CreateArchitectEndpoint(
        SignedInUserResolver users,
        CreateArchitectAuthorisation authorisation,
        CreateArchitectValidation validation,
        ICommandHandler<CreateArchitect, Architect> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateArchitect))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "architects")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateArchitect>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
