using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// PUT /api/requests/{requestId}/party — link the request to the party it is corresponded with.
/// Body: { "partyKind": 0|1, "partyId": "...", "onBehalfOfClientId": "..." } (a null/empty partyId
/// unlinks; onBehalfOfClientId only applies when the party is an architect).
/// </summary>
public sealed class LinkRequestToPartyEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly LinkRequestToPartyAuthorisation authorisation;
    private readonly LinkRequestToPartyValidation validation;
    private readonly ICommandHandler<LinkRequestToParty, Request> handler;

    public LinkRequestToPartyEndpoint(
        SignedInUserResolver users,
        LinkRequestToPartyAuthorisation authorisation,
        LinkRequestToPartyValidation validation,
        ICommandHandler<LinkRequestToParty, Request> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(LinkRequestToParty))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "requests/{requestId}/party")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<LinkRequestToParty>();
        if (body is null) return new BadRequestResult();

        var command = body with { RequestId = requestId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
