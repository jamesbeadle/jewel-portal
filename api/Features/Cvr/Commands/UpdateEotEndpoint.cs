using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateEotEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateEotAuthorisation authorisation;
    private readonly UpdateEotValidation validation;
    private readonly ICommandHandler<UpdateEot, Eot> handler;
    public UpdateEotEndpoint(SignedInUserResolver users, UpdateEotAuthorisation authorisation, UpdateEotValidation validation, ICommandHandler<UpdateEot, Eot> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateEot))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "eots/{eotId}")] HttpRequest request, string eotId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateEot>();
        if (command is null) return new BadRequestResult();
        if (command.EotId != eotId) return new BadRequestObjectResult("Route eotId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
