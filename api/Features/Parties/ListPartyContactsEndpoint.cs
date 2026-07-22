using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Parties;

public sealed class ListPartyContactsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPartyContacts, IReadOnlyList<PartyContact>> handler;

    public ListPartyContactsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListPartyContacts, IReadOnlyList<PartyContact>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Reading a party's contact book is open to all internal staff; managing it is gated
    // separately by PartyContactAuthorisation.
    private static readonly RoleSet RolesThatMayReadPartyContacts = JpmsRoleSets.AllInternal;

    [Function(nameof(ListPartyContacts))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "parties/{partyKind}/{partyId}/contacts")] HttpRequest request,
        string partyKind,
        string partyId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadPartyContacts.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var kind = PartyContactMapping.ParsePartyKind(partyKind);
        if (kind is null) return new BadRequestObjectResult("partyKind must be 'client' or 'architect'.");

        var contacts = await handler.HandleAsync(new ListPartyContacts(kind.Value, partyId), request.HttpContext.RequestAborted);
        return new OkObjectResult(contacts);
    }
}
