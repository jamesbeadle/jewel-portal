using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class UpdateRequestDetailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateRequestDetailsAuthorisation authorisation;
    private readonly UpdateRequestDetailsValidation validation;
    private readonly ICommandHandler<UpdateRequestDetails, Request> handler;
    public UpdateRequestDetailsEndpoint(SignedInUserResolver users, UpdateRequestDetailsAuthorisation authorisation, UpdateRequestDetailsValidation validation, ICommandHandler<UpdateRequestDetails, Request> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateRequestDetails))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "requests/{requestId}")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateRequestDetails>();
        if (command is null) return new BadRequestResult();
        if (command.RequestId != requestId) return new BadRequestObjectResult("Route requestId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
