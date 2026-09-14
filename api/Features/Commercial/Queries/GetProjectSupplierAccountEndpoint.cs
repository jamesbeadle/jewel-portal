using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// GET projects/{projectId}/suppliers/{subcontractorId}/account — the supplier's account on the
/// project (orders, invoices received, paid, left to invoice, over-invoice). The same internal
/// audience as the Work orders tab it opens from: every figure here is already on that tab or
/// the WO Allocation tab, folded into one supplier's picture.
/// </summary>
public sealed class GetProjectSupplierAccountEndpoint
{
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectSupplierAccount, ProjectSupplierAccount> handler;

    public GetProjectSupplierAccountEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetProjectSupplierAccount, ProjectSupplierAccount> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetProjectSupplierAccount))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/suppliers/{subcontractorId}/account")] HttpRequest request,
        string projectId,
        string subcontractorId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        try
        {
            var account = await handler.HandleAsync(new GetProjectSupplierAccount(projectId, subcontractorId), cancellationToken);
            return new OkObjectResult(account);
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }
    }
}
