using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CostCenters.Commands;

public sealed class AddCostCenterEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddCostCenterAuthorisation authorisation;
    private readonly AddCostCenterValidation validation;
    private readonly ICommandHandler<AddCostCenter, CostCenter> handler;

    public AddCostCenterEndpoint(
        SignedInUserResolver users,
        AddCostCenterAuthorisation authorisation,
        AddCostCenterValidation validation,
        ICommandHandler<AddCostCenter, CostCenter> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(AddCostCenter))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cost-centers")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<AddCostCenter>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var costCenter = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(costCenter);
        }
        catch (InvalidOperationException ex)
        {
            // Bare string so HttpCommandSender surfaces it verbatim in the dialog.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
