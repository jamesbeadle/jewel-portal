using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Actions;

/// <summary>
/// Lets triage handlers request mailbox side-effects (folder moves, outbound sends) without taking
/// a hard dependency on Graph or blocking on it. Work is enqueued and performed best-effort by a
/// worker; a failed side-effect never blocks the triage action — the DB is the source of truth,
/// the mailbox folder is a convenience mirror.
/// </summary>
public interface IMailboxActionScheduler
{
    /// <summary>Enqueue a move of the intake email into the folder for the given outcome status.</summary>
    Task ScheduleOutcomeMoveAsync(string intakeId, IntakeStatus status, CancellationToken ct);

    /// <summary>Enqueue an outbound (Shared) reply send for a request message.</summary>
    Task ScheduleOutboundSendAsync(string intakeId, string requestMessageId, CancellationToken ct);

    /// <summary>Enqueue a move of the intake email back to the Inbox (return-to-triage undo).</summary>
    Task ScheduleReturnToInboxAsync(string intakeId, CancellationToken ct);

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
    private readonly ILogger<MailboxActionScheduler> _logger;

    public MailboxActionScheduler(MailboxIntakeOptions options, IMailboxQueue queue, ILogger<MailboxActionScheduler> logger)
    {
        _options = options;
        _queue = queue;
        _logger = logger;
    }

    public Task ScheduleOutcomeMoveAsync(string intakeId, IntakeStatus status, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.EnableFolderMoves)
        {
            _logger.LogWarning(
                "Outcome move NOT enqueued for intake {IntakeId} ({Status}): Enabled={Enabled}, EnableFolderMoves={EnableFolderMoves}.",
                intakeId, status, _options.Enabled, _options.EnableFolderMoves);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Enqueuing outcome move for intake {IntakeId} ({Status}).", intakeId, status);
        return _queue.EnqueueMailboxActionAsync(
            new MailboxActionMessage(MailboxActionType.MoveToOutcomeFolder, intakeId, (int)status), ct);
    }

    public Task ScheduleOutboundSendAsync(string intakeId, string requestMessageId, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.EnableOutboundSend)
            return Task.CompletedTask;

        return _queue.EnqueueMailboxActionAsync(
            new MailboxActionMessage(MailboxActionType.SendOutbound, intakeId, RequestMessageId: requestMessageId), ct);
    }

    public Task ScheduleReturnToInboxAsync(string intakeId, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.EnableFolderMoves)
            return Task.CompletedTask;

        return _queue.EnqueueMailboxActionAsync(
            new MailboxActionMessage(MailboxActionType.ReturnToInbox, intakeId), ct);
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
