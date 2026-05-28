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

    [Function(nameof(ListComplianceDocumentsForSubcontractor))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/compliance")] HttpRequest request,
        string subcontractorId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListComplianceDocumentsForSubcontractor(subcontractorId), request.HttpContext.RequestAborted));
    }
}
