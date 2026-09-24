using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/variation-orders/{voId}/issued-date — the day the client was sent the
/// variation. Body: { issuedOn }.</summary>
public sealed class SetVariationIssuedDateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetVariationIssuedDateAuthorisation authorisation;
    private readonly SetVariationIssuedDateValidation validation;
    private readonly ICommandHandler<SetVariationIssuedDate, VariationOrder> handler;

    public SetVariationIssuedDateEndpoint(
        SignedInUserResolver users,
        SetVariationIssuedDateAuthorisation authorisation,
        SetVariationIssuedDateValidation validation,
        ICommandHandler<SetVariationIssuedDate, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetVariationIssuedDate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/issued-date")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SetVariationIssuedDate>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
