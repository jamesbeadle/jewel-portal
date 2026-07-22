using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class GetVariationOrderByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetVariationOrderById, VariationOrder?> handler;

    public GetVariationOrderByIdEndpoint(SignedInUserResolver users, IQueryHandler<GetVariationOrderById, VariationOrder?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Variation reads are internal plus the architect, who reads/approves variations per the permissions matrix.
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(GetVariationOrderById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "variation-orders/{voId}")] HttpRequest request,
        string voId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var vo = await handler.HandleAsync(new GetVariationOrderById(voId), request.HttpContext.RequestAborted);
        return new OkObjectResult(vo);
    }
}
