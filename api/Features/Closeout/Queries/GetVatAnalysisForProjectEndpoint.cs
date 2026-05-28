using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class GetVatAnalysisForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetVatAnalysisForProject, VatAnalysis?> handler;
    public GetVatAnalysisForProjectEndpoint(SignedInUserResolver users, IQueryHandler<GetVatAnalysisForProject, VatAnalysis?> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(GetVatAnalysisForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/vat")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new GetVatAnalysisForProject(projectId), request.HttpContext.RequestAborted));
    }
}
