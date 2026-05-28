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

    [Function(nameof(GetLeadQualification))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leads/{leadId}/qualification")] HttpRequest request,
        string leadId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var qualification = await handler.HandleAsync(new GetLeadQualification(leadId), request.HttpContext.RequestAborted);
        return new OkObjectResult(qualification);
    }
}
