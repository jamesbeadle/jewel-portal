using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CashCalls.Queries;

public sealed class GetProjectCashCallSummaryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectCashCallSummary, ProjectCashCallSummary> handler;

    public GetProjectCashCallSummaryEndpoint(SignedInUserResolver users, IQueryHandler<GetProjectCashCallSummary, ProjectCashCallSummary> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetProjectCashCallSummary))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cash-calls/summary")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var summary = await handler.HandleAsync(new GetProjectCashCallSummary(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(summary);
    }
}
