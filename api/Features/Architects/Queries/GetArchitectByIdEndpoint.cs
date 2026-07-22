using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Architects.Queries;

public sealed class GetArchitectByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetArchitectById, Architect?> handler;

    public GetArchitectByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetArchitectById, Architect?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Architect practice records are internal-only reads; managing them is gated separately
    // by ArchitectRoles.AllowedToManageArchitects.
    private static readonly RoleSet RolesThatMayReadArchitects = JpmsRoleSets.AllInternal;

    [Function(nameof(GetArchitectById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "architects/{architectId}")] HttpRequest request,
        string architectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadArchitects.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var architect = await handler.HandleAsync(new GetArchitectById(architectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(architect);
    }
}
