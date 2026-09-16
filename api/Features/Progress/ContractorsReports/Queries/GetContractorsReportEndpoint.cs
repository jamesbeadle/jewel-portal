using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Queries;

public sealed class GetContractorsReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetContractorsReport, ContractorsReportView?> handler;

    public GetContractorsReportEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetContractorsReport, ContractorsReportView?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetContractorsReport))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contractors-reports/{contractorsReportId}")] HttpRequest request,
        string contractorsReportId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var view = await handler.HandleAsync(new GetContractorsReport(contractorsReportId), request.HttpContext.RequestAborted);
        if (view is null) return new NotFoundObjectResult($"Contractor's Report {contractorsReportId} not found.");
        return new OkObjectResult(view);
    }
}
