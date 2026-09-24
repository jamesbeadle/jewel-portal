using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Registers.Policies;

/// <summary>Chasing one outstanding policy signature from the Policies page — the forms office's sign-in only.</summary>
public sealed class ChasePolicySignOffEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ChasePolicySignOffAuthorisation authorisation;
    private readonly ChasePolicySignOffValidation validation;
    private readonly ICommandHandler<ChasePolicySignOff, SentFormLink> handler;

    public ChasePolicySignOffEndpoint(
        SignedInUserResolver users, ChasePolicySignOffAuthorisation authorisation, ChasePolicySignOffValidation validation,
        ICommandHandler<ChasePolicySignOff, SentFormLink> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ChasePolicySignOff))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registers/policy-sign-offs/{policySignOffId}/chase")] HttpRequest request,
        string policySignOffId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new ChasePolicySignOff(policySignOffId, signedInUser.Email, signedInUser.DisplayName);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException refusal) { return new ConflictObjectResult(refusal.Message); }
    }
}
