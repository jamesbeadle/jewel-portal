using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// Mints, validates, rotates and revokes the connector's bearer tokens. The same hash-at-rest
/// contract as <see cref="SessionManager"/>: the raw secret leaves exactly once, in the token
/// response, and thereafter everything is looked up by SHA-256 hash.
/// </summary>
public sealed class OAuthTokenManager
{
    private readonly JpmsContext context;

    public OAuthTokenManager(JpmsContext context)
    {
        this.context = context;
    }

    public sealed record MintedTokens(string AccessToken, string RefreshToken, int ExpiresInSeconds);

    /// <summary>A valid (unexpired, unrevoked) access token's identity, else null.</summary>
    public sealed record ResolvedToken(string UserEmail, string ClientName, string TokenHash);

    /// <summary>Creates a fresh access + refresh pair for the user. The refresh token's hash is
    /// the family id, so one revoke can sweep every descendant.</summary>
    public async Task<MintedTokens> MintAsync(
        string userEmail, string clientId, string clientName, string scope, CancellationToken cancellationToken)
    {
        var accessSecret = AuthTokens.NewSecret();
        var refreshSecret = AuthTokens.NewSecret();
        var refreshHash = AuthTokens.Hash(refreshSecret);
        var now = DateTimeOffset.UtcNow;

        context.OAuthTokens.Add(new OAuthTokenEntity
        {
            TokenHash = AuthTokens.Hash(accessSecret),
            Kind = (int)OAuthDefaults.TokenKind.Access,
            UserEmail = userEmail,
            ClientId = clientId,
            ClientName = clientName,
            Scope = scope,
            FamilyId = refreshHash,
            IssuedAt = now,
            ExpiresAt = now.Add(OAuthDefaults.AccessTokenLifetime)
        });
        context.OAuthTokens.Add(new OAuthTokenEntity
        {
            TokenHash = refreshHash,
            Kind = (int)OAuthDefaults.TokenKind.Refresh,
            UserEmail = userEmail,
            ClientId = clientId,
            ClientName = clientName,
            Scope = scope,
            FamilyId = refreshHash,
            IssuedAt = now,
            ExpiresAt = now.Add(OAuthDefaults.RefreshTokenLifetime)
        });
        await context.SaveChangesAsync(cancellationToken);
        return new MintedTokens(accessSecret, refreshSecret, (int)OAuthDefaults.AccessTokenLifetime.TotalSeconds);
    }

    /// <summary>Rotates a refresh token: the old one is revoked and a new pair is minted in the
    /// same family. Null when the presented token is not a live refresh token.</summary>
    public async Task<MintedTokens?> RefreshAsync(string refreshSecret, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = AuthTokens.Hash(refreshSecret);
        var row = await context.OAuthTokens
            .FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (row is null
            || row.Kind != (int)OAuthDefaults.TokenKind.Refresh
            || row.RevokedAt is not null
            || row.ExpiresAt <= now)
            return null;

        row.RevokedAt = now;

        var accessSecret = AuthTokens.NewSecret();
        var newRefreshSecret = AuthTokens.NewSecret();
        var familyId = row.FamilyId ?? row.TokenHash;

        context.OAuthTokens.Add(new OAuthTokenEntity
        {
            TokenHash = AuthTokens.Hash(accessSecret),
            Kind = (int)OAuthDefaults.TokenKind.Access,
            UserEmail = row.UserEmail,
            ClientId = row.ClientId,
            ClientName = row.ClientName,
            Scope = row.Scope,
            FamilyId = familyId,
            IssuedAt = now,
            ExpiresAt = now.Add(OAuthDefaults.AccessTokenLifetime)
        });
        context.OAuthTokens.Add(new OAuthTokenEntity
        {
            TokenHash = AuthTokens.Hash(newRefreshSecret),
            Kind = (int)OAuthDefaults.TokenKind.Refresh,
            UserEmail = row.UserEmail,
            ClientId = row.ClientId,
            ClientName = row.ClientName,
            Scope = row.Scope,
            FamilyId = familyId,
            IssuedAt = now,
            ExpiresAt = now.Add(OAuthDefaults.RefreshTokenLifetime)
        });
        await context.SaveChangesAsync(cancellationToken);
        return new MintedTokens(accessSecret, newRefreshSecret, (int)OAuthDefaults.AccessTokenLifetime.TotalSeconds);
    }

    /// <summary>The identity behind a presented access token, else null. Bumps LastUsedAt at most
    /// once a minute so the connected-tools list can show recency without a write per call.</summary>
    public async Task<ResolvedToken?> ResolveAccessAsync(string accessSecret, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var hash = AuthTokens.Hash(accessSecret);
        var row = await context.OAuthTokens
            .FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        if (row is null
            || row.Kind != (int)OAuthDefaults.TokenKind.Access
            || row.RevokedAt is not null
            || row.ExpiresAt <= now)
            return null;

        if (row.LastUsedAt is null || now - row.LastUsedAt > TimeSpan.FromMinutes(1))
        {
            row.LastUsedAt = now;
            await context.SaveChangesAsync(cancellationToken);
        }
        return new ResolvedToken(row.UserEmail, row.ClientName, row.TokenHash);
    }

    /// <summary>Revokes every live token in the family — the "disconnect this tool" sweep.</summary>
    public async Task RevokeFamilyAsync(string familyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await context.OAuthTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var row in rows) row.RevokedAt = now;
        await context.SaveChangesAsync(cancellationToken);
    }
}
