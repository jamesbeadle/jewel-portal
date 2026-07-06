using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CostCenters.Commands;

public sealed class ReviseCostCenterEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReviseCostCenterAuthorisation authorisation;
    private readonly ReviseCostCenterValidation validation;
    private readonly ICommandHandler<ReviseCostCenter, CostCenter> handler;

    public ReviseCostCenterEndpoint(
        SignedInUserResolver users,
        ReviseCostCenterAuthorisation authorisation,
        ReviseCostCenterValidation validation,
        ICommandHandler<ReviseCostCenter, CostCenter> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReviseCostCenter))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cost-centers/{costCenterId}")] HttpRequest request,
        string costCenterId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<ReviseCostCenter>();
        if (body is null) return new BadRequestResult();
        var command = body with { CostCenterId = costCenterId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var costCenter = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(costCenter);
        }
        catch (KeyNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (InvalidOperationException ex)
        {
            // Bare string so HttpCommandSender surfaces it verbatim in the dialog.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
