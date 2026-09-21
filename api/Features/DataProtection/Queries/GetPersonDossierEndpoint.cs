using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection.Queries;

/// <summary>
/// GET /api/admin/people/dossier?email= — everything the portal holds against one address.
/// User administration, so it stays behind the admin gate with the directory commands.
/// </summary>
public sealed class GetPersonDossierEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetPersonDossier, PersonDossier> handler;

    public GetPersonDossierEndpoint(SignedInUserResolver users, IQueryHandler<GetPersonDossier, PersonDossier> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(GetPersonDossier))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/people/dossier")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        var email = request.Query["email"].ToString();
        if (string.IsNullOrWhiteSpace(email)) return new BadRequestObjectResult(new[] { "An email address is required." });

        var dossier = await handler.HandleAsync(new GetPersonDossier(email), request.HttpContext.RequestAborted);
        return new OkObjectResult(dossier);
    }
}
