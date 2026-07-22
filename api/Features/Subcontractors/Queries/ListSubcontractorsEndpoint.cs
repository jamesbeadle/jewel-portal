using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

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

    // The full company directory (with contact details) is internal-only. External sessions
    // (client, architect, subcontractor) get their own scoped views — a portal login reads its
    // own record via /portal/my/record, never the whole directory.
    private static readonly RoleSet InternalRolesThatMayListDirectory = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.Foreman);

    [Function(nameof(ListSubcontractors))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalRolesThatMayListDirectory.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListSubcontractors(), request.HttpContext.RequestAborted));
    }
}
