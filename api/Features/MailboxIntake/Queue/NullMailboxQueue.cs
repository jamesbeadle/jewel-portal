using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Queue;

/// <summary>No-op queue used when the mailbox feature is unconfigured. Logs and drops the work.</summary>
public sealed class NullMailboxQueue : IMailboxQueue
{
    private readonly ILogger<NullMailboxQueue> _logger;

    public NullMailboxQueue(ILogger<NullMailboxQueue> logger) => _logger = logger;

    public Task EnqueueIntakeNotificationAsync(string graphMessageId, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; dropping intake notification.");
        return Task.CompletedTask;
    }

    public Task EnqueueMailboxActionAsync(MailboxActionMessage action, CancellationToken ct)
    {
        _logger.LogDebug("Mailbox actions disabled; dropping {Type} for intake {IntakeId}.",
            action.Type, action.IntakeId);
        return Task.CompletedTask;
    }
}
