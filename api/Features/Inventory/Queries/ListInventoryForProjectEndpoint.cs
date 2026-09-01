using Jewel.JPMS.Contracts.Inventory;

namespace Jewel.JPMS.Api.Features.Inventory.Queries;

public sealed class ListInventoryForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListInventoryForProject, IReadOnlyList<InventoryItem>> handler;
    public ListInventoryForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListInventoryForProject, IReadOnlyList<InventoryItem>> handler) { this.users = users; this.handler = handler; }

    // Inventory reads are internal-only; external portal logins have no view here.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListInventoryForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/inventory")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListInventoryForProject(projectId), request.HttpContext.RequestAborted));
    }
}
