using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListProjectCostOfSalesLinesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProjectCostOfSalesLines, IReadOnlyList<ProjectCostOfSalesLine>> handler;

    public ListProjectCostOfSalesLinesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProjectCostOfSalesLines, IReadOnlyList<ProjectCostOfSalesLine>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListProjectCostOfSalesLines))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cost-of-sales-lines")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var lines = await handler.HandleAsync(new ListProjectCostOfSalesLines(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(lines);
    }
}
