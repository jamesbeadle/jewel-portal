using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// Hands out a live access token for the shared connection, refreshing through Bluebeam when the
/// stored one is inside its last five minutes (or on demand — the nightly keep-alive forces a
/// refresh so the rotating refresh token is exercised well inside Bluebeam's 7-unused-days limit).
/// Tokens persist on the connection ROW, not in memory: refresh tokens are single-use, and the api
/// and worker share only the database, so whichever process refreshed last must be the copy
/// everyone reads next. A failed refresh stamps the row so the admin page can say "reconnect".
/// </summary>
public sealed class BluebeamTokenService
{
    public const string ConnectionRowId = "bluebeam";
    private static readonly TimeSpan ExpiryHeadroom = TimeSpan.FromMinutes(5);

    private readonly JpmsContext context;
    private readonly IBluebeamClient client;

    public BluebeamTokenService(JpmsContext context, IBluebeamClient client)
    {
        this.context = context; this.client = client;
    }

    public Task<BluebeamConnectionEntity?> FindConnectionAsync(CancellationToken cancellationToken) =>
        context.BluebeamConnections
            .FirstOrDefaultAsync(row => row.BluebeamConnectionId == ConnectionRowId, cancellationToken);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        if (!client.IsConfigured)
            throw new BluebeamNotConnectedException(
                "Bluebeam isn't configured — add the Bluebeam__ClientId and Bluebeam__ClientSecret app settings.");

        var connection = await FindConnectionAsync(cancellationToken)
            ?? throw new BluebeamNotConnectedException(
                "Bluebeam isn't connected — an admin needs to connect it under Admin → Integrations.");

        var isStillFresh = connection.AccessToken is not null
            && connection.AccessTokenExpiresAt is { } expiresAt
            && DateTimeOffset.UtcNow < expiresAt - ExpiryHeadroom;
        if (isStillFresh && !forceRefresh) return connection.AccessToken!;

        return await RefreshAsync(connection, cancellationToken);
    }

    private async Task<string> RefreshAsync(BluebeamConnectionEntity connection, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await client.RefreshAsync(connection.RefreshToken, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            connection.AccessToken = tokens.AccessToken;
            connection.AccessTokenExpiresAt = now.AddSeconds(tokens.ExpiresInSeconds);
            // An empty rotated token would orphan the connection — keep the old one in that case.
            if (tokens.RefreshToken.Length > 0) connection.RefreshToken = tokens.RefreshToken;
            connection.RefreshTokenUpdatedAt = now;
            connection.LastRefreshSucceededAt = now;
            connection.LastRefreshError = null;
            await context.SaveChangesAsync(cancellationToken);
            return tokens.AccessToken;
        }
        catch (BluebeamCallFailedException failure)
        {
            // Refresh tokens are single-use, and parallel extraction runs can race to spend the
            // same one — the losers get a rejection even though the connection is healthy. Before
            // declaring the connection dead, re-read the row: a fresh token written by the winner
            // means this was only the race, not a dead grant.
            await context.Entry(connection).ReloadAsync(CancellationToken.None);
            var rescuedByAnotherRefresh = connection.AccessToken is not null
                && connection.AccessTokenExpiresAt is { } refreshedExpiry
                && DateTimeOffset.UtcNow < refreshedExpiry - ExpiryHeadroom;
            if (rescuedByAnotherRefresh) return connection.AccessToken!;

            connection.LastRefreshFailedAt = DateTimeOffset.UtcNow;
            connection.LastRefreshError = Trimmed(failure.Message);
            await context.SaveChangesAsync(CancellationToken.None);
            throw new BluebeamNotConnectedException(
                "Bluebeam refused the stored connection — an admin needs to reconnect it under Admin → Integrations. " + Trimmed(failure.Message));
        }
    }

    private static string Trimmed(string value) =>
        value.Length <= 1024 ? value : value[..1024];
}

/// <summary>The shared connection is missing, unconfigured, or dead — the message says what an
/// admin should do about it, and is safe to store on an extraction row.</summary>
public sealed class BluebeamNotConnectedException : Exception
{
    public BluebeamNotConnectedException(string message) : base(message) { }
}
