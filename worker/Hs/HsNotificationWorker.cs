using Jewel.JPMS.Api.Features.Hs.Notifications;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Worker.Hs;

/// <summary>
/// Every ten minutes: the H&amp;S digest sweep — a project's sitting of comments, status moves and
/// freshly minted actions is over once its last one is half an hour old, and then ONE email goes to
/// the site manager and ONE to each officer, never one per item (Katy-Louise, 15 Sep 2026).
/// </summary>
public sealed class HsNotificationWorker
{
    private readonly HsNotificationSweep sweep;
    private readonly ILogger<HsNotificationWorker> logger;

    public HsNotificationWorker(HsNotificationSweep sweep, ILogger<HsNotificationWorker> logger)
    {
        this.sweep = sweep;
        this.logger = logger;
    }

    [Function(nameof(HsNotificationWorker))]
    public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var outcome = await sweep.RunAsync(DateTimeOffset.UtcNow, cancellationToken);
        if (outcome.DigestsSent == 0 && outcome.EventsSpent == 0) return;
        logger.LogInformation("H&S digest: {Digests} email(s) sent, {Events} event(s) told.", outcome.DigestsSent, outcome.EventsSpent);
    }
}
