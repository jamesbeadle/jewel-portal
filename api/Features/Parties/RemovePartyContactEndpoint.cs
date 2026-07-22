using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Parties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Parties;

public sealed class RemovePartyContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PartyContactAuthorisation authorisation;
    private readonly ICommandHandler<RemovePartyContact, Acknowledgement> handler;

    public RemovePartyContactEndpoint(
        SignedInUserResolver users,
        PartyContactAuthorisation authorisation,
        ICommandHandler<RemovePartyContact, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(RemovePartyContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "parties/{partyKind}/{partyId}/contacts/{partyContactId}")] HttpRequest request,
        string partyKind,
        string partyId,
        string partyContactId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        var kind = PartyContactMapping.ParsePartyKind(partyKind);
        if (kind is null) return new BadRequestObjectResult("partyKind must be 'client' or 'architect'.");

        var ack = await handler.HandleAsync(
            new RemovePartyContact(kind.Value, partyId, partyContactId), request.HttpContext.RequestAborted);
        return new OkObjectResult(ack);
    }
}
