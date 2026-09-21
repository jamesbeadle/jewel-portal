using Jewel.JPMS.Contracts.Architects;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

/// <summary>
/// POST /api/architects/{architectId}/portal-invite — invites the practice's contact (or an
/// override email) to the portal. Mints the standard set-password link and links the login to
/// the practice so architect writes scope to its projects. Returns the copyable link
/// (InviteResult). The architect twin of InviteClientPortalUserEndpoint.
/// </summary>
public sealed class InviteArchitectPortalUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly InviteArchitectPortalUserAuthorisation authorisation;
    private readonly InviteArchitectPortalUserHandler handler;
    private readonly IConfiguration configuration;

    public InviteArchitectPortalUserEndpoint(
        SignedInUserResolver users, InviteArchitectPortalUserAuthorisation authorisation,
        InviteArchitectPortalUserHandler handler, IConfiguration configuration)
    {
        this.users = users; this.authorisation = authorisation; this.handler = handler; this.configuration = configuration;
    }

    [Function("InviteArchitectPortalUser")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "architects/{architectId}/portal-invite")] HttpRequest request,
        string architectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        InviteArchitectPortalUserRequest? body;
        try { body = await request.ReadFromJsonAsync<InviteArchitectPortalUserRequest>(cancellationToken); }
        catch { body = null; }
        body ??= new InviteArchitectPortalUserRequest();

        var outcome = await handler.InviteAsync(
            architectId, body.Email, body.DisplayName, ResolveSiteBaseUrl(request), cancellationToken);
        if (outcome.Result is not null) return new OkObjectResult(outcome.Result);
        return new ObjectResult(new { error = outcome.Error }) { StatusCode = outcome.StatusCode };
    }

    private string ResolveSiteBaseUrl(HttpRequest request)
    {
        var configured = configuration["PublicSiteUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
        return $"{request.Scheme}://{request.Host.Value}";
    }
}
