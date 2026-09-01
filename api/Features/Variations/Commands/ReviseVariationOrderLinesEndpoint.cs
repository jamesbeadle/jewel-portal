using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/revise-lines — re-state an approved variation's priced lines.
/// Body: { lines }. The reviser is the signed-in user.
/// </summary>
public sealed class ReviseVariationOrderLinesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReviseVariationOrderLinesAuthorisation authorisation;
    private readonly ReviseVariationOrderLinesValidation validation;
    private readonly ICommandHandler<ReviseVariationOrderLines, VariationOrder> handler;

    public ReviseVariationOrderLinesEndpoint(
        SignedInUserResolver users,
        ReviseVariationOrderLinesAuthorisation authorisation,
        ReviseVariationOrderLinesValidation validation,
        ICommandHandler<ReviseVariationOrderLines, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReviseVariationOrderLines))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/revise-lines")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<ReviseVariationOrderLines>(cancellationToken);
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId, RevisedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's guards (not approved, nothing to price, value settled on a claim) are
            // answers to what was asked, not faults — 400 so the dialog shows the reason next to
            // the lines rather than the client falling back to "Backend call failure" on a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
