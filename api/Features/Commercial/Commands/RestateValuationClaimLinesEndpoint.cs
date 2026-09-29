using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>POST /api/valuation-claims/{claimId}/restatement — move money between a locked claim's lines, total unchanged.</summary>
public sealed class RestateValuationClaimLinesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RestateValuationClaimLinesAuthorisation authorisation;
    private readonly RestateValuationClaimLinesValidation validation;
    private readonly ICommandHandler<RestateValuationClaimLines, IReadOnlyList<ClaimLine>> handler;
    public RestateValuationClaimLinesEndpoint(SignedInUserResolver users, RestateValuationClaimLinesAuthorisation authorisation, RestateValuationClaimLinesValidation validation, ICommandHandler<RestateValuationClaimLines, IReadOnlyList<ClaimLine>> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RestateValuationClaimLines))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-claims/{claimId}/restatement")] HttpRequest request, string claimId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<RestateValuationClaimLines>();
        if (command is null) return new BadRequestResult();
        if (command.ValuationClaimId != claimId) return new BadRequestObjectResult("Route claimId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(refusal.Message);
        }
    }
}
