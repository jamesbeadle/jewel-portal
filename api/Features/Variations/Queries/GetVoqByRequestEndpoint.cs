using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVoqByRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetVoqByRequest, VariationOrderQuote?> handler;

    public GetVoqByRequestEndpoint(SignedInUserResolver users, IQueryHandler<GetVoqByRequest, VariationOrderQuote?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Variation reads are internal plus the architect, who reads/approves variations per the permissions matrix.
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(GetVoqByRequest))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/voq")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var voq = await handler.HandleAsync(new GetVoqByRequest(requestId), request.HttpContext.RequestAborted);
        return new OkObjectResult(voq);
    }
}
