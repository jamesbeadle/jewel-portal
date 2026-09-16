using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

public sealed class CreateContractorsReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateContractorsReportAuthorisation authorisation;
    private readonly CreateContractorsReportValidation validation;
    private readonly ICommandHandler<CreateContractorsReport, ContractorsReport> handler;

    public CreateContractorsReportEndpoint(
        SignedInUserResolver users,
        CreateContractorsReportAuthorisation authorisation,
        CreateContractorsReportValidation validation,
        ICommandHandler<CreateContractorsReport, ContractorsReport> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateContractorsReport))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/contractors-reports")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateContractorsReport>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        command = command with { CreatedByEmail = signedInUser.Email };
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var report = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(report);
        }
        catch (InvalidOperationException refused)
        {
            return new ConflictObjectResult(refused.Message);
        }
    }
}
