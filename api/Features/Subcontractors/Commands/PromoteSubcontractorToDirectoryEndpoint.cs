using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class PromoteSubcontractorToDirectoryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PromoteSubcontractorToDirectoryAuthorisation authorisation;
    private readonly PromoteSubcontractorToDirectoryValidation validation;
    private readonly ICommandHandler<PromoteSubcontractorToDirectory, Subcontractor> handler;

    public PromoteSubcontractorToDirectoryEndpoint(
        SignedInUserResolver users,
        PromoteSubcontractorToDirectoryAuthorisation authorisation,
        PromoteSubcontractorToDirectoryValidation validation,
        ICommandHandler<PromoteSubcontractorToDirectory, Subcontractor> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(PromoteSubcontractorToDirectory))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/{subcontractorId}/promote")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<PromoteSubcontractorToDirectory>();
        if (command is null) return new BadRequestResult();
        if (command.SubcontractorId != subcontractorId) return new BadRequestObjectResult("Route subcontractorId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
