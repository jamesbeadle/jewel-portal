using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

public sealed class UpdateContractorsReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateContractorsReportAuthorisation authorisation;
    private readonly UpdateContractorsReportValidation validation;
    private readonly ICommandHandler<UpdateContractorsReport, ContractorsReport> handler;

    public UpdateContractorsReportEndpoint(
        SignedInUserResolver users,
        UpdateContractorsReportAuthorisation authorisation,
        UpdateContractorsReportValidation validation,
        ICommandHandler<UpdateContractorsReport, ContractorsReport> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateContractorsReport))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "contractors-reports/{contractorsReportId}")] HttpRequest request,
        string contractorsReportId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateContractorsReport>();
        if (command is null) return new BadRequestResult();
        if (command.ContractorsReportId != contractorsReportId) return new BadRequestObjectResult("Route contractorsReportId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var report = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(report);
    }
}
