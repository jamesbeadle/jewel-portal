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

    [Function(nameof(ListClaimLines))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "valuation-claims/{claimId}/entries")] HttpRequest request, string claimId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListClaimLines(claimId), request.HttpContext.RequestAborted));
    }
}
