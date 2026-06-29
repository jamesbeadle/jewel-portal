using Jewel.JPMS.Api.Features.MailboxIntake.Queue;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Actions;

/// <summary>
/// Lets handlers request mailbox side-effects that must run out-of-band — sending the rendered
/// request document and Shared outbound replies — without blocking on Graph. Work is enqueued and
/// performed best-effort by the worker. (Folder moves are no longer scheduled here: in the live-read
/// model triage moves happen synchronously in the API against the mailbox.)
/// </summary>
public interface IMailboxActionScheduler
{
    /// <summary>
    /// Enqueue sending a request's rendered document (RFI etc.) to the project's flagged contacts.
    /// Pass <paramref name="recipientOverride"/> to email a single ad-hoc address instead (a resend).
    /// The PDF is regenerated from SQL by the worker, so this carries only the request id.
    /// </summary>
    Task ScheduleRequestDocumentSendAsync(string requestId, string? recipientOverride, CancellationToken ct);
}

/// <summary>Default scheduler: enqueues onto the mailbox-actions queue, honouring the feature flags.</summary>
public sealed class MailboxActionScheduler : IMailboxActionScheduler
{
    private readonly MailboxIntakeOptions _options;
    private readonly IMailboxQueue _queue;

    public MailboxActionScheduler(MailboxIntakeOptions options, IMailboxQueue queue)
    {
        _options = options;
        _queue = queue;
    }

    public Task ScheduleRequestDocumentSendAsync(string requestId, string? recipientOverride, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.EnableRequestDocuments)
            return Task.CompletedTask;

        return _queue.EnqueueMailboxActionAsync(
            new MailboxActionMessage(
                MailboxActionType.SendRequestDocument,
                IntakeId: string.Empty,
                RequestId: requestId,
                RecipientOverride: recipientOverride),
            ct);
    }
}
