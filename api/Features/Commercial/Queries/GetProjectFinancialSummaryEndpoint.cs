using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class GetProjectFinancialSummaryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>> handler;

    public GetProjectFinancialSummaryEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetProjectFinancialSummary))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/financial-summary")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var rows = await handler.HandleAsync(new GetProjectFinancialSummary(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(rows);
    }
}
