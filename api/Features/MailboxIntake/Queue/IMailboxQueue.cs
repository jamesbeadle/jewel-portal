namespace Jewel.JPMS.Api.Features.MailboxIntake.Queue;

/// <summary>
/// Enqueues mailbox work so it runs out-of-band with retries + dead-lettering, never inline with
/// a triage action. A no-op fallback is used when the feature is unconfigured.
/// </summary>
public interface IMailboxQueue
{
    /// <summary>Enqueue a webhook notification (a Graph message id to fetch + ingest).</summary>
    Task EnqueueIntakeNotificationAsync(string graphMessageId, CancellationToken ct);

    /// <summary>Enqueue a mailbox side-effect (folder move / outbound send).</summary>
    Task EnqueueMailboxActionAsync(MailboxActionMessage action, CancellationToken ct);
}
