using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// Persists the outcome of a successful connect: the single connection row (upserted) and the
/// audit record. Shared source — the callback lives on the WORKER Function App (the Static Web
/// Apps edge intercepts any request carrying a ?code= query parameter as one of its own auth
/// callbacks and 500s it, so the SWA can never safely receive an OAuth redirect), and the api
/// keeps a fallback callback; both write through this one class so they cannot drift.
/// </summary>
public sealed class BluebeamConnectionWriter
{
    private readonly JpmsContext context;
    private readonly ILogger<BluebeamConnectionWriter> logger;

    public BluebeamConnectionWriter(JpmsContext context, ILogger<BluebeamConnectionWriter> logger)
    {
        this.context = context; this.logger = logger;
    }

    public async Task StoreAsync(
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
        // The callback arrives unauthenticated — the admin's identity rides in the signed state
        // minted when they clicked Connect.
        connection.ConnectedBy = adminEmail;
        connection.ConnectedAt = now;
        connection.RefreshTokenUpdatedAt = now;
        connection.LastRefreshSucceededAt = now;
        connection.LastRefreshFailedAt = null;
        connection.LastRefreshError = null;
        await context.SaveChangesAsync(cancellationToken);
    }

    // Best-effort, after the connection is safely stored — an audit hiccup must never fail it.
    public async Task WriteConnectedAuditAsync(string adminEmail, CancellationToken cancellationToken)
    {
        try
        {
            context.AuditEvents.Add(new AuditEventEntity
            {
                AuditEventId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTimeOffset.UtcNow,
                ActorEmail = adminEmail,
                EventType = (int)AuditEventType.BluebeamConnected,
                Pathway = "",
                RecordReference = "",
                Detail = "Connected the shared Bluebeam Studio account"
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Audit write failed for the Bluebeam connect.");
        }
    }
}
