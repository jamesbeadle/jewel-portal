using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// POST /api/valuation-claims/{claimId}/statement-email — email the locked valuation's statement
/// (PDF attached) to the client side from the shared mailbox, or stage it as a draft
/// (saveAsDraftOnly). Body: { subject, htmlBody, saveAsDraftOnly }.
/// POST /api/valuation-report-snapshots/{snapshotId}/draft-email is the retired address
/// (2026-09-18); a snapshot id there resolves to its claim.
/// </summary>
public sealed class SendValuationStatementEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendValuationStatementEmailAuthorisation authorisation;
    private readonly SendValuationStatementEmailValidation validation;
    private readonly ICommandHandler<SendValuationStatementEmail, ValuationStatementEmailOutcome> handler;

    public SendValuationStatementEmailEndpoint(
        SignedInUserResolver users,
        SendValuationStatementEmailAuthorisation authorisation,
        SendValuationStatementEmailValidation validation,
        ICommandHandler<SendValuationStatementEmail, ValuationStatementEmailOutcome> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SendValuationStatementEmail))]
    public Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-claims/{claimId}/statement-email")] HttpRequest request,
        string claimId) => RespondAsync(request, claimId);

    [Function("SendValuationStatementEmailByRetiredSnapshotId")]
    public Task<IActionResult> RunLegacy(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuation-report-snapshots/{snapshotId}/draft-email")] HttpRequest request,
        string snapshotId) => RespondAsync(request, snapshotId);

    private async Task<IActionResult> RespondAsync(HttpRequest request, string claimId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<SendValuationStatementEmail>();
        if (body is null) return new BadRequestResult();
        var command = body with { ValuationClaimId = claimId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
