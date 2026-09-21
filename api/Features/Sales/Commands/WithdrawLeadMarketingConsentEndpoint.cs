using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// POST sales/leads/{leadId}/marketing-consent/withdraw — the sales team recording a prospect's
// "please stop" (data protection, 2026-09-21).
public sealed class WithdrawLeadMarketingConsentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly WithdrawLeadMarketingConsentAuthorisation authorisation;
    private readonly WithdrawLeadMarketingConsentValidation validation;
    private readonly ICommandHandler<WithdrawLeadMarketingConsent, Lead> handler;

    public WithdrawLeadMarketingConsentEndpoint(
        SignedInUserResolver users,
        WithdrawLeadMarketingConsentAuthorisation authorisation,
        WithdrawLeadMarketingConsentValidation validation,
        ICommandHandler<WithdrawLeadMarketingConsent, Lead> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(WithdrawLeadMarketingConsent))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/marketing-consent/withdraw")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new WithdrawLeadMarketingConsent(leadId, signedInUser.Email);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);

        try { return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted)); }
        catch (InvalidOperationException refusal) { return new BadRequestObjectResult(refusal.Message); }
    }
}
