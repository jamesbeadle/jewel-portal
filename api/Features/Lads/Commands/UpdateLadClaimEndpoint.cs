using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Lads.Commands;

public sealed class UpdateLadClaimEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateLadClaimAuthorisation authorisation;
    private readonly UpdateLadClaimValidation validation;
    private readonly ICommandHandler<UpdateLadClaim, LadClaim> handler;

    public UpdateLadClaimEndpoint(SignedInUserResolver users, UpdateLadClaimAuthorisation authorisation, UpdateLadClaimValidation validation, ICommandHandler<UpdateLadClaim, LadClaim> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateLadClaim))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "lad-claims/{ladClaimId}")] HttpRequest request, string ladClaimId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateLadClaim>();
        if (posted is null) return new BadRequestResult();
        if (posted.LadClaimId != ladClaimId) return new BadRequestObjectResult("Route ladClaimId does not match body.");

        if (!authorisation.Allows(signedInUser, posted)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(posted);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(posted, request.HttpContext.RequestAborted));
    }
}
