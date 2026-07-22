using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class UpdateDefectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateDefectAuthorisation authorisation;
    private readonly UpdateDefectValidation validation;
    private readonly ICommandHandler<UpdateDefect, Defect> handler;
    public UpdateDefectEndpoint(SignedInUserResolver users, UpdateDefectAuthorisation authorisation, UpdateDefectValidation validation, ICommandHandler<UpdateDefect, Defect> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateDefect))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "defects/{defectId}")] HttpRequest request, string defectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateDefect>();
        if (command is null) return new BadRequestResult();
        if (command.DefectId != defectId) return new BadRequestObjectResult("Route defectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
