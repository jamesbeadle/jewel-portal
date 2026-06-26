using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Subscriptions;

/// <summary>
/// Runs well inside the subscription expiry window (every 12h, plus on startup) to create or renew
/// the Graph change-notification subscription. Recreation on lapse is handled by the manager.
/// </summary>
public sealed class MailboxSubscriptionTimer
{
    private readonly MailboxSubscriptionManager _manager;
    private readonly ILogger<MailboxSubscriptionTimer> _logger;

    public MailboxSubscriptionTimer(MailboxSubscriptionManager manager, ILogger<MailboxSubscriptionTimer> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [Function(nameof(MailboxSubscriptionTimer))]
    public async Task Run([TimerTrigger("0 0 */12 * * *", RunOnStartup = true)] TimerInfo timer, CancellationToken ct)
    {
        try
        {
            await _manager.EnsureSubscriptionAsync(ct);
        }
        catch (Exception ex)
        {
            // Never let a subscription hiccup crash the host; the delta sweep keeps completeness.
            _logger.LogError(ex, "Mailbox subscription ensure failed.");
        }
    }
}
