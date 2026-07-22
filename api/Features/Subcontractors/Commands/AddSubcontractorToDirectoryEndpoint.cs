using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddSubcontractorToDirectoryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddSubcontractorToDirectoryAuthorisation authorisation;
    private readonly AddSubcontractorToDirectoryValidation validation;
    private readonly ICommandHandler<AddSubcontractorToDirectory, Subcontractor> handler;

    public AddSubcontractorToDirectoryEndpoint(SignedInUserResolver users, AddSubcontractorToDirectoryAuthorisation authorisation, AddSubcontractorToDirectoryValidation validation, ICommandHandler<AddSubcontractorToDirectory, Subcontractor> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(AddSubcontractorToDirectory))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<AddSubcontractorToDirectory>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
