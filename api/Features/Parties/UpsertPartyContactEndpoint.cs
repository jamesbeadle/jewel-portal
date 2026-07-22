using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Parties;

public sealed class UpsertPartyContactEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PartyContactAuthorisation authorisation;
    private readonly UpsertPartyContactValidation validation;
    private readonly ICommandHandler<UpsertPartyContact, PartyContact> handler;

    public UpsertPartyContactEndpoint(
        SignedInUserResolver users,
        PartyContactAuthorisation authorisation,
        UpsertPartyContactValidation validation,
        ICommandHandler<UpsertPartyContact, PartyContact> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpsertPartyContact))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "parties/{partyKind}/{partyId}/contacts")] HttpRequest request,
        string partyKind,
        string partyId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var kind = PartyContactMapping.ParsePartyKind(partyKind);
        if (kind is null) return new BadRequestObjectResult("partyKind must be 'client' or 'architect'.");

        var command = await request.ReadFromJsonAsync<UpsertPartyContact>();
        if (command is null) return new BadRequestResult();
        if (command.PartyId != partyId || command.PartyKind != kind.Value)
            return new BadRequestObjectResult("Route party does not match body.");

        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
