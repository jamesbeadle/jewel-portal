using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetClientCostReferencesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly SetClientCostReferencesValidation validation;
    private readonly ICommandHandler<SetClientCostReferences, IReadOnlyList<ClientCostReference>> handler;

    public SetClientCostReferencesEndpoint(
        SignedInUserResolver users,
        ValuationReportAuthorisation authorisation,
        SetClientCostReferencesValidation validation,
        ICommandHandler<SetClientCostReferences, IReadOnlyList<ClientCostReference>> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(SetClientCostReferences))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{projectId}/client-cost-references")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<SetClientCostReferences>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
