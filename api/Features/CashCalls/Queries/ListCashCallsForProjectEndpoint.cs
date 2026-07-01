using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CashCalls.Queries;

public sealed class ListCashCallsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCashCallsForProject, IReadOnlyList<CashCall>> handler;

    public ListCashCallsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListCashCallsForProject, IReadOnlyList<CashCall>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCashCallsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cash-calls")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var calls = await handler.HandleAsync(new ListCashCallsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(calls);
    }
}
