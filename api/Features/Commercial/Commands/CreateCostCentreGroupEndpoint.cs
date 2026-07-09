using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class CreateCostCentreGroupEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateCostCentreGroupAuthorisation authorisation;
    private readonly CreateCostCentreGroupValidation validation;
    private readonly ICommandHandler<CreateCostCentreGroup, CostCentreGroup> handler;

    public CreateCostCentreGroupEndpoint(
        SignedInUserResolver users,
        CreateCostCentreGroupAuthorisation authorisation,
        CreateCostCentreGroupValidation validation,
        ICommandHandler<CreateCostCentreGroup, CostCentreGroup> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateCostCentreGroup))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/cost-centre-groups")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateCostCentreGroup>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            var group = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(group);
        }
        catch (InvalidOperationException ex)
        {
            // e.g. a centre already sits in another group — show the handler's message verbatim.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
