using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListCostCodeBudgetsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCostCodeBudgetsForProject, IReadOnlyList<CostCodeBudget>> handler;
    public ListCostCodeBudgetsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListCostCodeBudgetsForProject, IReadOnlyList<CostCodeBudget>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListCostCodeBudgetsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cost-code-budgets")] HttpRequest request, string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListCostCodeBudgetsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
