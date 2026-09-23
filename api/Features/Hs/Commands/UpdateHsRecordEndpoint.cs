using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class UpdateHsRecordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly UpdateHsRecordAuthorisation authorisation;
    private readonly UpdateHsRecordValidation validation;
    private readonly ICommandHandler<UpdateHsRecord, HsRecord> handler;

    public UpdateHsRecordEndpoint(
        SignedInUserResolver users, JpmsContext context, UpdateHsRecordAuthorisation authorisation,
        UpdateHsRecordValidation validation, ICommandHandler<UpdateHsRecord, HsRecord> handler)
    {
        this.users = users;
        this.context = context;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateHsRecord))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "hs-records/{hsRecordId}")] HttpRequest request, string hsRecordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<UpdateHsRecord>();
        if (posted is null) return new BadRequestResult();
        if (posted.HsRecordId != hsRecordId) return new BadRequestObjectResult("Route hsRecordId does not match body.");
        var command = posted with { ChangedByEmail = signedInUser.Email, ChangedByName = signedInUser.DisplayName };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var mayClose = await HsRecordCloseScope.AllowsAsync(context, signedInUser, command, request.HttpContext.RequestAborted);
        if (!mayClose) return new ObjectResult(HsRecordCloseScope.Refusal) { StatusCode = 403 };
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
