using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Queries;

public sealed class RevealUsefulInformationSecretEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UsefulInformationOptions options;
    private readonly IQueryHandler<RevealUsefulInformationSecret, UsefulInformationSecret> handler;

    public RevealUsefulInformationSecretEndpoint(SignedInUserResolver users, UsefulInformationOptions options, IQueryHandler<RevealUsefulInformationSecret, UsefulInformationSecret> handler)
    { this.users = users; this.options = options; this.handler = handler; }

    [Function(nameof(RevealUsefulInformationSecret))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "useful-information-notes/{noteId}/secret")] HttpRequest request, string noteId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        // Directors only — the one gate that ever answers the value. Everyone else on staff sees
        // that a credential is held (HasSecret on the list) and nothing more.
        if (!UsefulInformationRoles.AllowedToReveal.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!options.CanHoldSecrets) return new BadRequestObjectResult("The portal has no credential key configured.");

        return new OkObjectResult(await handler.HandleAsync(new RevealUsefulInformationSecret(noteId), request.HttpContext.RequestAborted));
    }
}
