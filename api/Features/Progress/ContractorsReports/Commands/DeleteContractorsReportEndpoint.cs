using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

public sealed class DeleteContractorsReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteContractorsReportAuthorisation authorisation;
    private readonly ICommandHandler<DeleteContractorsReport, Acknowledgement> handler;

    public DeleteContractorsReportEndpoint(
        SignedInUserResolver users,
        DeleteContractorsReportAuthorisation authorisation,
        ICommandHandler<DeleteContractorsReport, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(DeleteContractorsReport))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "contractors-reports/{contractorsReportId}")] HttpRequest request,
        string contractorsReportId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new DeleteContractorsReport(contractorsReportId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var acknowledgement = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(acknowledgement);
    }
}
