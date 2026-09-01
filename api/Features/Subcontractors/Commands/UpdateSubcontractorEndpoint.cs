using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpdateSubcontractorEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateSubcontractorAuthorisation authorisation;
    private readonly UpdateSubcontractorValidation validation;
    private readonly ICommandHandler<UpdateSubcontractor, Subcontractor> handler;

    public UpdateSubcontractorEndpoint(SignedInUserResolver users, UpdateSubcontractorAuthorisation authorisation, UpdateSubcontractorValidation validation, ICommandHandler<UpdateSubcontractor, Subcontractor> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(UpdateSubcontractor))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "subcontractors/{subcontractorId}")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateSubcontractor>();
        if (command is null) return new BadRequestResult();
        if (command.SubcontractorId != subcontractorId) return new BadRequestObjectResult("Route subcontractorId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's guards (record gone, last trade being removed, a trade id outside the
            // curated list) are answers to what was asked, not faults — 400 so the dialog shows the
            // reason next to the field instead of the client falling back to the host's opaque
            // "Backend call failure" on a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
