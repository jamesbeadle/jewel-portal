using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListCompanyContactsEndpoint
{
    // Same audience as the directory list itself — the contacts are the record's contact details.
    private static readonly RoleSet InternalRolesThatMayListDirectory = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.Foreman);

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCompanyContacts, IReadOnlyList<CompanyContact>> handler;

    public ListCompanyContactsEndpoint(SignedInUserResolver users, IQueryHandler<ListCompanyContacts, IReadOnlyList<CompanyContact>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCompanyContacts))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/contacts")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalRolesThatMayListDirectory.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListCompanyContacts(subcontractorId), request.HttpContext.RequestAborted));
    }
}
