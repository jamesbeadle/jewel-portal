using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Jewel.JPMS.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Actions;

/// <summary>
/// Performs queued mailbox side-effects out-of-band: moving an email into its outcome folder as
/// triage progresses, and (when enabled) sending Shared outbound replies via Graph. These are
/// best-effort and retried by the queue; the DB remains the source of truth.
/// </summary>
public sealed class MailboxActionWorker
{
    private readonly JpmsContext _context;
    private readonly IGraphMailClient _graph;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<MailboxActionWorker> _logger;

    public MailboxActionWorker(
        JpmsContext context, IGraphMailClient graph, MailboxIntakeOptions options, ILogger<MailboxActionWorker> logger)
    {
        _context = context;
        _graph = graph;
        _options = options;
        _logger = logger;
    }

    [Function(nameof(MailboxActionWorker))]
    public async Task Run(
        [QueueTrigger(MailboxQueues.MailboxActions, Connection = "MailboxQueuesConnection")] MailboxActionMessage action,
        CancellationToken ct)
    {
        switch (action.Type)
        {
            case MailboxActionType.MoveToOutcomeFolder:
                await MoveAsync(action, ct);
                break;
            case MailboxActionType.SendOutbound:
                await SendAsync(action, ct);
                break;
            default:
                _logger.LogWarning("Unknown mailbox action type {Type}.", action.Type);
                break;
        }
    }

    private async Task MoveAsync(MailboxActionMessage action, CancellationToken ct)
    {
        if (!_options.EnableFolderMoves)
            return;

        var intake = await _context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == action.IntakeId, ct);
        if (intake is null)
        {
            _logger.LogWarning("Move skipped: intake {IntakeId} not found.", action.IntakeId);
            return;
        }

        if (string.IsNullOrEmpty(intake.GraphMessageId))
        {
            _logger.LogWarning("Move skipped: intake {IntakeId} has no Graph message id.", action.IntakeId);
            return;
        }

        var status = (IntakeStatus)(action.TargetStatus ?? intake.Status);
        var folderId = OutcomeFolders.ResolveFolderId(status, _options.Folders);
        if (string.IsNullOrEmpty(folderId))
        {
            _logger.LogDebug("No outcome folder configured for status {Status}; leaving in place.", status);
            return;
        }

        // The id changes on move — persist the new one so future moves/replies use the right handle.
        var newGraphId = await _graph.MoveMessageAsync(intake.GraphMessageId, folderId, ct);
        intake.GraphMessageId = newGraphId;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Moved intake {IntakeId} to folder for {Status}.", action.IntakeId, status);
    }

    private async Task SendAsync(MailboxActionMessage action, CancellationToken ct)
    {
        if (!_options.EnableOutboundSend || string.IsNullOrEmpty(action.RequestMessageId))
            return;

        var message = await _context.RequestMessages
            .FirstOrDefaultAsync(m => m.MessageId == action.RequestMessageId, ct);
        if (message is null)
        {
            _logger.LogWarning("Outbound send skipped: message {MessageId} not found.", action.RequestMessageId);
            return;
        }

        // Hard guard: internal messages must NEVER be emailed.
        if (message.Visibility != (int)MessageVisibility.Shared || message.Direction != (int)MessageDirection.Outbound)
        {
            _logger.LogWarning("Refusing to email message {MessageId}: not a Shared outbound message.", message.MessageId);
            return;
        }

        var intake = await _context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == action.IntakeId, ct);
        if (intake is null || string.IsNullOrEmpty(intake.GraphMessageId))
        {
            message.SentStatus = (int)MessageSentStatus.Failed;
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning("Outbound send failed: no Graph thread handle for intake {IntakeId}.", action.IntakeId);
            return;
        }

        try
        {
            await _graph.ReplyAsync(intake.GraphMessageId, message.Body, ct);
            message.SentStatus = (int)MessageSentStatus.Sent;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Sent outbound reply for message {MessageId}.", message.MessageId);
        }
        catch (Exception)
        {
            message.SentStatus = (int)MessageSentStatus.Failed;
            await _context.SaveChangesAsync(ct);
            throw; // let the queue retry; persistent failures surface in the poison queue.
        }
    }
}
