using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CostCenters.Queries;

public sealed class ListCostCentersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCostCenters, IReadOnlyList<CostCenter>> handler;
    public ListCostCentersEndpoint(SignedInUserResolver users, IQueryHandler<ListCostCenters, IReadOnlyList<CostCenter>> handler) { this.users = users; this.handler = handler; }

    // Cost centres are internal reference data; managing them is gated separately by the
    // CostCenters command authorisations.
    private static readonly RoleSet RolesThatMayReadCostCenters = JpmsRoleSets.AllInternal;

    [Function(nameof(ListCostCenters))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cost-centers")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadCostCenters.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var includeInactive = string.Equals(request.Query["includeInactive"], "true", StringComparison.OrdinalIgnoreCase);
        return new OkObjectResult(await handler.HandleAsync(new ListCostCenters(includeInactive), request.HttpContext.RequestAborted));
    }
}
