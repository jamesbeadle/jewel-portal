using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Mobilisation.Commands;

public sealed class UpdateMobilisationChecklistItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateMobilisationChecklistItemAuthorisation authorisation;
    private readonly UpdateMobilisationChecklistItemValidation validation;
    private readonly ICommandHandler<UpdateMobilisationChecklistItem, MobilisationItem> handler;

    public UpdateMobilisationChecklistItemEndpoint(SignedInUserResolver users, UpdateMobilisationChecklistItemAuthorisation authorisation, UpdateMobilisationChecklistItemValidation validation, ICommandHandler<UpdateMobilisationChecklistItem, MobilisationItem> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateMobilisationChecklistItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "mobilisation-items/{mobilisationItemId}")] HttpRequest request, string mobilisationItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateMobilisationChecklistItem>();
        if (command is null) return new BadRequestResult();
        if (command.MobilisationItemId != mobilisationItemId) return new BadRequestObjectResult("Route mobilisationItemId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
