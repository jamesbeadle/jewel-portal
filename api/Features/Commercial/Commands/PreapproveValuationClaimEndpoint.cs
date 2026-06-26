using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class PreapproveValuationClaimEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly ICommandHandler<PreapproveValuationClaim, ValuationClaim> handler;
    public PreapproveValuationClaimEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, ICommandHandler<PreapproveValuationClaim, ValuationClaim> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(PreapproveValuationClaim))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-claims/{claimId}/preapproval")] HttpRequest request, string claimId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new PreapproveValuationClaim(claimId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
