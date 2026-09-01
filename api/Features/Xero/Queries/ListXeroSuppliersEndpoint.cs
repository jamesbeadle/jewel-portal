using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

public sealed class ListXeroSuppliersEndpoint
{
    // Mirrors the import gate (directory managers): the list exists to feed "Import from Xero",
    // and it exposes supplier contact details straight from the accounts system.
    private static readonly RoleSet AllowedToListSuppliers = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot> handler;

    public ListXeroSuppliersEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListXeroSuppliers))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/suppliers")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToListSuppliers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var force = string.Equals(request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);
        var snapshot = await handler.HandleAsync(new ListXeroSuppliers(force), request.HttpContext.RequestAborted);
        return new OkObjectResult(snapshot);
    }
}
