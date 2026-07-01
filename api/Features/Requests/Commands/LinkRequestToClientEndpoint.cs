using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// PUT /api/requests/{requestId}/client — link the request to a client account. Body: { "clientId": "..." }
/// (a null/empty clientId unlinks).
/// </summary>
public sealed class LinkRequestToClientEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly LinkRequestToClientAuthorisation authorisation;
    private readonly LinkRequestToClientValidation validation;
    private readonly ICommandHandler<LinkRequestToClient, Request> handler;

    public LinkRequestToClientEndpoint(
        SignedInUserResolver users,
        LinkRequestToClientAuthorisation authorisation,
        LinkRequestToClientValidation validation,
        ICommandHandler<LinkRequestToClient, Request> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(LinkRequestToClient))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "requests/{requestId}/client")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<LinkRequestToClient>();
        if (body is null) return new BadRequestResult();

        var command = body with { RequestId = requestId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
