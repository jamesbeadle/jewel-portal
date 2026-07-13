using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListComplianceDocumentsForSubcontractorEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>> handler;

    public ListComplianceDocumentsForSubcontractorEndpoint(SignedInUserResolver users, IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal roles may read any record's documents; a portal-scoped subcontractor login may
    // only read its own (the route param is never trusted for external sessions).
    private static readonly RoleSet InternalRolesThatMayReadCompliance = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator);

    [Function(nameof(ListComplianceDocumentsForSubcontractor))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/compliance")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        if (!InternalRolesThatMayReadCompliance.IncludesAny(signedInUser.Roles))
        {
            var ownSubcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
            if (ownSubcontractorId is null
                || !string.Equals(ownSubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
                return new ForbidResult();
        }

        return new OkObjectResult(await handler.HandleAsync(new ListComplianceDocumentsForSubcontractor(subcontractorId), request.HttpContext.RequestAborted));
    }
}
