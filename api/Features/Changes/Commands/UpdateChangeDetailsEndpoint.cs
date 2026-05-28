using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class UpdateChangeDetailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateChangeDetailsAuthorisation authorisation;
    private readonly UpdateChangeDetailsValidation validation;
    private readonly ICommandHandler<UpdateChangeDetails, ChangeRecord> handler;
    public UpdateChangeDetailsEndpoint(SignedInUserResolver users, UpdateChangeDetailsAuthorisation authorisation, UpdateChangeDetailsValidation validation, ICommandHandler<UpdateChangeDetails, ChangeRecord> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateChangeDetails))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "changes/{changeRecordId}")] HttpRequest request, string changeRecordId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateChangeDetails>();
        if (command is null) return new BadRequestResult();
        if (command.ChangeRecordId != changeRecordId) return new BadRequestObjectResult("Route changeRecordId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
