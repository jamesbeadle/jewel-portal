using Jewel.JPMS.Api.Features.Bluebeam;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Worker.Bluebeam;

/// <summary>
/// The refresh-token keep-alive. Bluebeam kills a refresh token after 7 days unused, so this
/// forces a refresh every night at 03:15 UTC (before the Xero run at 04:30) — six missed nights
/// of slack before the connection actually dies. Outcomes land on the connection row
/// (LastRefreshSucceededAt / LastRefreshError), which Admin → Integrations surfaces; a dead
/// token's only remedy is an admin reconnecting, so failing loudly here would help nobody.
/// </summary>
public sealed class BluebeamTokenRefreshWorker
{
    private readonly BluebeamTokenService tokens;
    private readonly IBluebeamClient client;
    private readonly ILogger<BluebeamTokenRefreshWorker> logger;

    public BluebeamTokenRefreshWorker(
        BluebeamTokenService tokens, IBluebeamClient client, ILogger<BluebeamTokenRefreshWorker> logger)
    {
        this.tokens = tokens; this.client = client; this.logger = logger;
    }

    [Function(nameof(BluebeamTokenRefreshWorker))]
    public async Task Run([TimerTrigger("0 15 3 * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        if (!client.IsConfigured)
        {
            logger.LogInformation("Bluebeam is not configured — keep-alive skipped.");
            return;
        }
        var connection = await tokens.FindConnectionAsync(cancellationToken);
        if (connection is null)
        {
            logger.LogInformation("Bluebeam is not connected — keep-alive skipped.");
            return;
        }

        try
        {
            await tokens.GetAccessTokenAsync(cancellationToken, forceRefresh: true);
            logger.LogInformation("Bluebeam refresh token exercised.");
        }
        catch (BluebeamNotConnectedException failure)
        {
            // Already stamped on the row by the token service — the admin page shows it.
            logger.LogWarning("Bluebeam keep-alive failed: {Message}", failure.Message);
        }
    }
}
