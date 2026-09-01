using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Connect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// The consent page's two calls. GET /api/oauth/client-info names the asking client; POST
/// /api/oauth/approve records the signed-in user's decision and mints the single-use code the
/// AI tool exchanges for tokens. Both require the portal session cookie — the identity minted
/// into the code is the session's, never anything the page sent.
/// </summary>
public sealed class ApproveAuthorizationEndpoint
{
    private readonly JpmsContext context;
    private readonly SignedInUserResolver users;

    public ApproveAuthorizationEndpoint(JpmsContext context, SignedInUserResolver users)
    {
        this.context = context;
        this.users = users;
    }

    [Function("OAuthClientInfo")]
    public async Task<IActionResult> ClientInfo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oauth/client-info")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = request.Query["client_id"].ToString();
        var client = await context.OAuthClients.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ClientId == clientId, cancellationToken);
        if (client is null) return new NotFoundResult();
        return new OkObjectResult(new ConnectClientInfo(client.ClientId, client.ClientName));
    }

    [Function("OAuthApprove")]
    public async Task<IActionResult> Approve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oauth/approve")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        ApproveConnectRequest? body;
        try { body = await request.ReadFromJsonAsync<ApproveConnectRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }
        if (body is null) return new BadRequestResult();

        // Re-validate everything the consent page carried — the query string it was opened with
        // is not trusted.
        var client = await context.OAuthClients.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ClientId == body.ClientId, cancellationToken);
        if (client is null) return new BadRequestObjectResult("Unknown client.");
        var registeredUris = JsonSerializer.Deserialize<List<string>>(client.RedirectUrisJson) ?? new List<string>();
        if (!registeredUris.Contains(body.RedirectUri, StringComparer.Ordinal))
            return new BadRequestObjectResult("redirect_uri is not registered for this client.");

        if (!body.Approved)
            return new OkObjectResult(new ApproveConnectResponse(
                Redirect(body.RedirectUri, new QueryBuilder { { "error", "access_denied" } }, body.State)));

        if (string.IsNullOrEmpty(body.CodeChallenge) || body.CodeChallenge.Length > 128)
            return new BadRequestObjectResult("Missing PKCE code challenge.");

        var code = AuthTokens.NewSecret();
        var now = DateTimeOffset.UtcNow;
        context.OAuthAuthCodes.Add(new OAuthAuthCodeEntity
        {
            CodeHash = AuthTokens.Hash(code),
            ClientId = client.ClientId,
            UserEmail = signedInUser.Email,
            RedirectUri = body.RedirectUri,
            CodeChallenge = body.CodeChallenge,
            Scope = string.IsNullOrWhiteSpace(body.Scope) ? OAuthDefaults.Scope : body.Scope,
            Resource = string.IsNullOrWhiteSpace(body.Resource) ? null : body.Resource,
            CreatedAt = now,
            ExpiresAt = now.Add(OAuthDefaults.CodeLifetime)
        });
        await context.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new ApproveConnectResponse(
            Redirect(body.RedirectUri, new QueryBuilder { { "code", code } }, body.State)));
    }

    private static string Redirect(string redirectUri, QueryBuilder query, string state)
    {
        if (!string.IsNullOrEmpty(state)) query.Add("state", state);
        return $"{redirectUri}{query}";
    }
}
