using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// The one-time connect flow. Start (admin-only) mints the consent URL the browser is sent to;
/// the callback is where Bluebeam's redirect lands — necessarily anonymous, because it arrives as
/// a bare browser GET, so the signed ten-minute state minted by Start is what proves the flow was
/// begun by an admin here (the Connect feature's OAuth endpoints are the house precedent for
/// hand-rolled redirects like this). Either way the browser ends up back on /admin/integrations.
/// </summary>
public sealed class BluebeamConnectEndpoints
{
    private const string AdminPagePath = "/admin/integrations";

    private readonly SignedInUserResolver users;
    private readonly BluebeamOptions options;
    private readonly IBluebeamClient client;
    private readonly JpmsContext context;
    private readonly AuditTrail auditTrail;
    private readonly ILogger<BluebeamConnectEndpoints> logger;

    public BluebeamConnectEndpoints(
        SignedInUserResolver users, BluebeamOptions options, IBluebeamClient client,
        JpmsContext context, AuditTrail auditTrail, ILogger<BluebeamConnectEndpoints> logger)
    {
        this.users = users; this.options = options; this.client = client;
        this.context = context; this.auditTrail = auditTrail; this.logger = logger;
    }

    [Function("StartBluebeamConnect")]
    public async Task<IActionResult> Start(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bluebeam/connect/start")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);
        if (!options.IsConfigured)
            return new BadRequestObjectResult(
                "Bluebeam isn't configured — add the Bluebeam__ClientId and Bluebeam__ClientSecret app settings first.");

        // response_mode=form_post makes Bluebeam's sign-in POST the code back instead of putting
        // it in the query string — the Static Web Apps edge intercepts ANY request carrying a
        // ?code= parameter as one of its own auth callbacks and 500s it before our function runs
        // (found the hard way, 2026-08-31). A POST body sails through untouched.
        var state = BluebeamConnectionState.Mint(options.ClientSecret!, signedInUser.Email);
        var authorizeUrl = options.AuthorizeUrl
            + $"?response_type=code&response_mode=form_post"
            + $"&client_id={Uri.EscapeDataString(options.ClientId!)}"
            + $"&redirect_uri={Uri.EscapeDataString(options.RedirectUri)}"
            + $"&scope={Uri.EscapeDataString(options.Scopes)}"
            + $"&state={Uri.EscapeDataString(state)}";
        return new OkObjectResult(new BluebeamConnectStart(authorizeUrl));
    }

    [Function("BluebeamConnectCallback")]
    public async Task<IActionResult> Callback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "bluebeam/callback")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        if (!options.IsConfigured) return Failed("not-configured");
        // form_post delivers code + state in the POST body; the query fallback keeps a plain GET
        // callback working should the response mode ever be ignored.
        var state = await ReadParameterAsync(request, "state");
        var adminEmail = BluebeamConnectionState.VerifiedAdminEmail(state, options.ClientSecret!);
        if (adminEmail is null) return Failed("bad-state");
        var code = await ReadParameterAsync(request, "code");
        if (string.IsNullOrWhiteSpace(code)) return Failed("no-code");

        try
        {
            var tokens = await client.ExchangeCodeAsync(code, cancellationToken);
            var connectedUser = await ReadConnectedUserAsync(tokens.AccessToken, cancellationToken);
            await StoreConnectionAsync(tokens, connectedUser, adminEmail, cancellationToken);
        }
        catch (BluebeamCallFailedException)
        {
            return Failed("exchange-failed");
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            // A browser redirect must never end on a bare 500 — the person can't see logs. The
            // usual culprit here is the store write (e.g. the migration not applied yet).
            logger.LogError(failure, "Bluebeam connect callback failed after code exchange.");
            return Failed("server-error");
        }

        await auditTrail.WriteAsync(
            AuditEventType.BluebeamConnected,
            "Connected the shared Bluebeam Studio account",
            actorEmail: adminEmail,
            cancellationToken: cancellationToken);
        return new RedirectResult($"{AdminPagePath}?bluebeam=connected");
    }

    // The identity read is a nicety — a connection whose /users/me shape surprises us is still a
    // working connection, so any failure just leaves the email blank.
    private async Task<BluebeamUser> ReadConnectedUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        try { return await client.GetCurrentUserAsync(accessToken, cancellationToken); }
        catch (BluebeamCallFailedException) { return new BluebeamUser("", ""); }
    }

    private async Task StoreConnectionAsync(
        BluebeamTokens tokens, BluebeamUser connectedUser, string adminEmail, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var connection = await context.BluebeamConnections
            .FirstOrDefaultAsync(row => row.BluebeamConnectionId == BluebeamTokenService.ConnectionRowId, cancellationToken);
        if (connection is null)
        {
            connection = new BluebeamConnectionEntity { BluebeamConnectionId = BluebeamTokenService.ConnectionRowId };
            context.BluebeamConnections.Add(connection);
        }
        connection.RefreshToken = tokens.RefreshToken;
        connection.AccessToken = tokens.AccessToken;
        connection.AccessTokenExpiresAt = now.AddSeconds(tokens.ExpiresInSeconds);
        connection.ConnectedEmail = connectedUser.Email;
        // The callback itself arrives unauthenticated — the admin's identity rides in the signed
        // state minted when they clicked Connect.
        connection.ConnectedBy = adminEmail;
        connection.ConnectedAt = now;
        connection.RefreshTokenUpdatedAt = now;
        connection.LastRefreshSucceededAt = now;
        connection.LastRefreshFailedAt = null;
        connection.LastRefreshError = null;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string> ReadParameterAsync(HttpRequest request, string name)
    {
        var fromQuery = request.Query[name].ToString();
        if (fromQuery.Length > 0) return fromQuery;
        if (!request.HasFormContentType) return "";
        var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
        return form[name].ToString();
    }

    private static RedirectResult Failed(string reason) =>
        new($"{AdminPagePath}?bluebeam=failed&reason={reason}");
}
