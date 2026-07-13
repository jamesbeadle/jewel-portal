using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetLeadQualificationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetLeadQualification, QualificationAssessment?> handler;

    public GetLeadQualificationEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetLeadQualification, QualificationAssessment?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Lead pipeline reads are internal-only; external portal logins have no business here.
    private static readonly RoleSet RolesThatMayReadLeads = JpmsRoleSets.AllInternal;

    [Function(nameof(GetLeadQualification))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/qualification")] HttpRequest request,
        string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadLeads.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var qualification = await handler.HandleAsync(new GetLeadQualification(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(qualification);
    }
}
