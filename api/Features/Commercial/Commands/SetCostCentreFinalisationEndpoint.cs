using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCentreFinalisationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetCostCentreFinalisationAuthorisation authorisation;
    private readonly SetCostCentreFinalisationValidation validation;
    private readonly ICommandHandler<SetCostCentreFinalisation, CostCentreCostProgress> handler;

    public SetCostCentreFinalisationEndpoint(
        SignedInUserResolver users,
        SetCostCentreFinalisationAuthorisation authorisation,
        SetCostCentreFinalisationValidation validation,
        ICommandHandler<SetCostCentreFinalisation, CostCentreCostProgress> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetCostCentreFinalisation))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/cost-centre-finalisation")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetCostCentreFinalisation>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to lock or unlock cost centres.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var progress = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(progress);
    }
}
