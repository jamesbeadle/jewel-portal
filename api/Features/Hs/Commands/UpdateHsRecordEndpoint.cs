using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class UpdateHsRecordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateHsRecordAuthorisation authorisation;
    private readonly UpdateHsRecordValidation validation;
    private readonly ICommandHandler<UpdateHsRecord, HsRecord> handler;

    public UpdateHsRecordEndpoint(SignedInUserResolver users, UpdateHsRecordAuthorisation authorisation, UpdateHsRecordValidation validation, ICommandHandler<UpdateHsRecord, HsRecord> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateHsRecord))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "hs-records/{hsRecordId}")] HttpRequest request, string hsRecordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateHsRecord>();
        if (command is null) return new BadRequestResult();
        if (command.HsRecordId != hsRecordId) return new BadRequestObjectResult("Route hsRecordId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
