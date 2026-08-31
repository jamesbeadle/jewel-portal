using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListCurrentComplianceDocumentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>> handler;

    public ListCurrentComplianceDocumentsEndpoint(SignedInUserResolver users, IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // The whole-company read is internal-only: a portal-scoped subcontractor login reads its own
    // documents through the per-record route, never everyone's. Mirrors the per-record endpoint's
    // internal role set.
    private static readonly RoleSet InternalRolesThatMayReadCompliance = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    [Function(nameof(ListCurrentComplianceDocuments))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "compliance-documents")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalRolesThatMayReadCompliance.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(new ListCurrentComplianceDocuments(), request.HttpContext.RequestAborted));
    }
}
