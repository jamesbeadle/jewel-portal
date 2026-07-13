using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// POST /api/subcontractors/{subcontractorId}/portal-invite — invites the record's contact (or an
/// override email) to the subcontractor portal. Mints the standard set-password link and links the
/// login to the record so portal endpoints scope to it. Returns the copyable link (InviteResult).
/// </summary>
public sealed class InviteSubcontractorPortalUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly InviteSubcontractorPortalUserAuthorisation authorisation;
    private readonly SubcontractorPortalInviter inviter;
    private readonly IConfiguration configuration;

    public InviteSubcontractorPortalUserEndpoint(
        SignedInUserResolver users, InviteSubcontractorPortalUserAuthorisation authorisation,
        SubcontractorPortalInviter inviter, IConfiguration configuration)
    {
        this.users = users; this.authorisation = authorisation; this.inviter = inviter; this.configuration = configuration;
    }

    [Function("InviteSubcontractorPortalUser")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/{subcontractorId}/portal-invite")] HttpRequest request,
        string subcontractorId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new ForbidResult();

        InviteSubcontractorPortalUserRequest? body;
        try { body = await request.ReadFromJsonAsync<InviteSubcontractorPortalUserRequest>(cancellationToken); }
        catch { body = null; }
        body ??= new InviteSubcontractorPortalUserRequest();

        var outcome = await inviter.InviteAsync(
            subcontractorId, body.Email, body.DisplayName, ResolveSiteBaseUrl(request), cancellationToken);

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
