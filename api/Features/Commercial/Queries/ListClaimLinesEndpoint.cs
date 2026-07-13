using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListClaimLinesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListClaimLines, IReadOnlyList<ClaimLine>> handler;
    public ListClaimLinesEndpoint(SignedInUserResolver users, IQueryHandler<ListClaimLines, IReadOnlyList<ClaimLine>> handler) { this.users = users; this.handler = handler; }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListClaimLines))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-claims/{claimId}/entries")] HttpRequest request, string claimId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListClaimLines(claimId), request.HttpContext.RequestAborted));
    }
}
