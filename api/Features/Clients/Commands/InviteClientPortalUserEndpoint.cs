using Jewel.JPMS.Contracts.Clients;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

/// <summary>
/// POST /api/clients/{clientId}/portal-invite — invites the account's primary contact (or an
/// override email) to the client portal. Mints the standard set-password link and links the
/// login to the account so client-portal endpoints scope to it. Returns the copyable link
/// (InviteResult). The client twin of InviteSubcontractorPortalUserEndpoint.
/// </summary>
public sealed class InviteClientPortalUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly InviteClientPortalUserAuthorisation authorisation;
    private readonly ClientPortalInviter inviter;
    private readonly IConfiguration configuration;

    public InviteClientPortalUserEndpoint(
        SignedInUserResolver users, InviteClientPortalUserAuthorisation authorisation,
        ClientPortalInviter inviter, IConfiguration configuration)
    {
        this.users = users; this.authorisation = authorisation; this.inviter = inviter; this.configuration = configuration;
    }

    [Function("InviteClientPortalUser")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "clients/{clientId}/portal-invite")] HttpRequest request,
        string clientId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        InviteClientPortalUserRequest? body;
        try { body = await request.ReadFromJsonAsync<InviteClientPortalUserRequest>(cancellationToken); }
        catch { body = null; }
        body ??= new InviteClientPortalUserRequest();

        var outcome = await inviter.InviteAsync(
            clientId, body.Email, body.DisplayName, ResolveSiteBaseUrl(request), cancellationToken);

        if (outcome.Result is not null) return new OkObjectResult(outcome.Result);
        return new ObjectResult(new { error = outcome.Error }) { StatusCode = outcome.StatusCode };
    }

    /// <summary>Mirrors InviteUserEndpoint: prefer the configured PublicSiteUrl so set-password
    /// links survive being served from the raw Function App host.</summary>
    private string ResolveSiteBaseUrl(HttpRequest request)
    {
        var configured = configuration["PublicSiteUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
        return $"{request.Scheme}://{request.Host.Value}";
    }
}
