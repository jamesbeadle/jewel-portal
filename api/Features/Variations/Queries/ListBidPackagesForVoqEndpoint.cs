using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListBidPackagesForVoqEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListBidPackagesForVoq, IReadOnlyList<BidPackage>> handler;

    public ListBidPackagesForVoqEndpoint(SignedInUserResolver users, IQueryHandler<ListBidPackagesForVoq, IReadOnlyList<BidPackage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Variation reads are internal plus the architect, who reads/approves variations per the permissions matrix.
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(ListBidPackagesForVoq))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "voqs/{voqId}/bid-packages")] HttpRequest request,
        string voqId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var packages = await handler.HandleAsync(new ListBidPackagesForVoq(voqId), request.HttpContext.RequestAborted);
        return new OkObjectResult(packages);
    }
}
