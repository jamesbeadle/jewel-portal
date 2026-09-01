using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListUnallocatedSiteBillsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListUnallocatedSiteBills, IReadOnlyList<UnallocatedSiteBill>> handler;

    public ListUnallocatedSiteBillsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListUnallocatedSiteBills, IReadOnlyList<UnallocatedSiteBill>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListUnallocatedSiteBills))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/unallocated-site-bills")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var bills = await handler.HandleAsync(new ListUnallocatedSiteBills(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(bills);
    }
}
