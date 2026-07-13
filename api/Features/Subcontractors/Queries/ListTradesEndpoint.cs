using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListTradesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTrades, IReadOnlyList<Trade>> handler;

    public ListTradesEndpoint(SignedInUserResolver users, IQueryHandler<ListTrades, IReadOnlyList<Trade>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // The trade taxonomy is internal reference data; portal sessions do not browse it.
    private static readonly RoleSet RolesThatMayReadTrades = JpmsRoleSets.AllInternal;

    [Function(nameof(ListTrades))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "trades")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadTrades.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTrades(), request.HttpContext.RequestAborted));
    }
}
