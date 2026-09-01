using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Connect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// The profile page's "Connected AI tools" list and its revoke button. A connection is a live
/// refresh-token family; revoking sweeps the whole family, so the tool's next call — access or
/// refresh — fails and it has to sign in again. Users see their own; Admins may revoke anyone's
/// (mirrors how sessions are administered in the directory).
/// </summary>
public sealed class ConnectionsEndpoints
{
    private readonly JpmsContext context;
    private readonly SignedInUserResolver users;
    private readonly OAuthTokenManager tokens;

    public ConnectionsEndpoints(JpmsContext context, SignedInUserResolver users, OAuthTokenManager tokens)
    {
        this.context = context;
        this.users = users;
        this.tokens = tokens;
    }

    [Function("ListAiConnections")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oauth/connections")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var listAll = request.Query["all"] == "true" && signedInUser.Roles.Contains(Role.Admin);
        var now = DateTimeOffset.UtcNow;

        var families = await context.OAuthTokens.AsNoTracking()
            .Where(row => row.RevokedAt == null && row.ExpiresAt > now)
            .Where(row => listAll || row.UserEmail == signedInUser.Email)
            .GroupBy(row => row.FamilyId!)
            .Select(family => new
            {
                FamilyId = family.Key,
                ClientName = family.Max(row => row.ClientName),
                UserEmail = family.Max(row => row.UserEmail),
                ConnectedAt = family.Min(row => row.IssuedAt),
                LastUsedAt = family.Max(row => row.LastUsedAt)
            })
            .OrderByDescending(family => family.ConnectedAt)
            .ToListAsync(cancellationToken);

        return new OkObjectResult(families
            .Select(family => new AiConnection(
                family.FamilyId, family.ClientName, family.UserEmail, family.ConnectedAt, family.LastUsedAt))
            .ToList());
    }

    [Function("RevokeAiConnection")]
    public async Task<IActionResult> Revoke(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oauth/connections/revoke")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        RevokeAiConnectionRequest? body;
        try { body = await request.ReadFromJsonAsync<RevokeAiConnectionRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }
        if (body is null || string.IsNullOrEmpty(body.FamilyId)) return new BadRequestResult();

        var owner = await context.OAuthTokens.AsNoTracking()
            .Where(row => row.FamilyId == body.FamilyId)
            .Select(row => row.UserEmail)
            .FirstOrDefaultAsync(cancellationToken);
        if (owner is null) return new NotFoundResult();
        if (owner != signedInUser.Email && !signedInUser.Roles.Contains(Role.Admin))
            return new StatusCodeResult(403);

        await tokens.RevokeFamilyAsync(body.FamilyId, cancellationToken);
        return new OkResult();
    }
}
