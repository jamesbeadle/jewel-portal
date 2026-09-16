using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Queries;

public sealed class ListContractorsReportsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListContractorsReports, IReadOnlyList<ContractorsReport>> handler;

    public ListContractorsReportsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListContractorsReports, IReadOnlyList<ContractorsReport>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListContractorsReports))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contractors-reports")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var reports = await handler.HandleAsync(new ListContractorsReports(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(reports);
    }
}
