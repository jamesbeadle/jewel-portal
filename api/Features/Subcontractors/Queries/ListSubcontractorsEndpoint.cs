using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListSubcontractorsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>> handler;

    public ListSubcontractorsEndpoint(SignedInUserResolver users, IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListSubcontractors))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DirectoryRoles.AllowedToList.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListSubcontractors(), request.HttpContext.RequestAborted));
    }
}
