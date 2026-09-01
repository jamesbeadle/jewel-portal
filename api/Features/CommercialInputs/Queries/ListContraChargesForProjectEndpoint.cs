using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListContraChargesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListContraChargesForProject, IReadOnlyList<ContraCharge>> handler;

    public ListContraChargesForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListContraChargesForProject, IReadOnlyList<ContraCharge>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Commercial input reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListContraChargesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contra-charges")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var contraCharges = await handler.HandleAsync(new ListContraChargesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(contraCharges);
    }
}
