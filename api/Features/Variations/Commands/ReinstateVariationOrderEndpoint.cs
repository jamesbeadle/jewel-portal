using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/variation-orders/{voId}/reinstate — bring a rejected variation order back
/// to Issued (or Quoting when it was never issued).</summary>
public sealed class ReinstateVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReinstateVariationOrderAuthorisation authorisation;
    private readonly ReinstateVariationOrderValidation validation;
    private readonly ICommandHandler<ReinstateVariationOrder, VariationOrder> handler;

    public ReinstateVariationOrderEndpoint(
        SignedInUserResolver users,
        ReinstateVariationOrderAuthorisation authorisation,
        ReinstateVariationOrderValidation validation,
        ICommandHandler<ReinstateVariationOrder, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ReinstateVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/reinstate")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new ReinstateVariationOrder(voId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
