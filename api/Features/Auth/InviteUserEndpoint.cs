using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Auth;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// POST /api/auth/invite (admin only) — creates or updates a directory user with the chosen roles,
/// ensures they have an invited credential and emails them a single-use "set your password" link.
/// The link is also returned to the admin. Re-inviting issues a fresh link without disturbing any
/// password the user has already set.
/// </summary>
public sealed class InviteUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UserInviter inviter;
    private readonly IConfiguration configuration;

    public InviteUserEndpoint(SignedInUserResolver users, UserInviter inviter, IConfiguration configuration)
    {
        this.users = users;
        this.inviter = inviter;
        this.configuration = configuration;
    }

    [Function("AuthInviteUser")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/invite")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        InviteUserRequest? body;
        try { body = await request.ReadFromJsonAsync<InviteUserRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }
        if (body is null) return new BadRequestResult();

        var email = body.Email?.Trim() ?? "";
        if (!LooksLikeEmail(email))
            return new BadRequestObjectResult(new { error = "Enter a valid email address." });

        var displayName = string.IsNullOrWhiteSpace(body.DisplayName) ? email : body.DisplayName.Trim();
        var roles = (body.Roles ?? Array.Empty<Role>()).Distinct().ToList();
        var baseUrl = ResolveSiteBaseUrl(request);

        var result = await inviter.InviteAsync(email, displayName, roles, baseUrl, cancellationToken);
        return new OkObjectResult(result);
    }

    /// <summary>The public site the set-password link should point at. Prefers the configured
    /// PublicSiteUrl so links survive being served from the raw Function App host.</summary>
    private string ResolveSiteBaseUrl(HttpRequest request)
    {
        var configured = configuration["PublicSiteUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
        return $"{request.Scheme}://{request.Host.Value}";
    }

    private static bool LooksLikeEmail(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@') && value.IndexOf('@') < value.LastIndexOf('.');
}
