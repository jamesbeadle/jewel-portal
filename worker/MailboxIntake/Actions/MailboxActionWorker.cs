using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Documents;
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
            case MailboxActionType.ReturnToInbox:
                await ReturnToInboxAsync(action, ct);
                break;
            case MailboxActionType.SendRequestDocument:
                await SendRequestDocumentAsync(action, ct);
                break;
            default:
                _logger.LogWarning("Unknown mailbox action type {Type}.", action.Type);
                break;
        }
    }

    private async Task MoveAsync(MailboxActionMessage action, CancellationToken ct)
    {
        if (!_options.EnableFolderMoves)
        {
            _logger.LogWarning("Move skipped for intake {IntakeId}: EnableFolderMoves is off in the worker config.", action.IntakeId);
            return;
        }

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
        var folderId = await ResolveFolderAsync(intake, status, ct);
        if (string.IsNullOrEmpty(folderId))
        {
            _logger.LogWarning("No outcome folder resolved for status {Status}; leaving intake {IntakeId} in place.", status, action.IntakeId);
            return;
        }

        // Guard against a same-folder move: Graph's /move duplicates a message when the destination
        // is the folder it already lives in. If it's already there, there is nothing to do.
        var currentFolderId = await _graph.GetMessageParentFolderIdAsync(intake.GraphMessageId, ct);
        _logger.LogInformation(
            "Move intake {IntakeId} for {Status}: current folder {CurrentFolder}, target folder {TargetFolder}.",
            action.IntakeId, status, currentFolderId, folderId);
        if (!string.IsNullOrEmpty(currentFolderId) && string.Equals(currentFolderId, folderId, StringComparison.Ordinal))
        {
            _logger.LogInformation("Intake {IntakeId} already in the folder for {Status}; no move needed.", action.IntakeId, status);
            return;
        }

        // The id changes on move — persist the new one so future moves/replies use the right handle.
        var newGraphId = await _graph.MoveMessageAsync(intake.GraphMessageId, folderId, ct);
        intake.GraphMessageId = newGraphId;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Moved intake {IntakeId} to folder for {Status}.", action.IntakeId, status);
    }

    /// <summary>
    /// Resolves the destination folder for a triage outcome:
    ///   Linked    -> the request's own subfolder (REQ-0001) under the "Requests" parent, created on first use;
    ///   Discarded -> a folder under the Inbox (defaults to "General"), found-or-created on demand;
    ///   otherwise -> any statically-configured outcome folder (e.g. Failed -> Needs attention) as a safety net.
    /// Returns null when there is nothing to move to (e.g. unconfigured Graph, or a Linked email with no request).
    /// </summary>
    private async Task<string?> ResolveFolderAsync(IntakeEmailEntity intake, IntakeStatus status, CancellationToken ct)
    {
        switch (status)
        {
            case IntakeStatus.Linked:
            {
                if (string.IsNullOrEmpty(intake.LinkedRequestId))
                {
                    _logger.LogWarning("Move skipped: intake {IntakeId} is Linked but has no request id.", intake.IntakeId);
                    return null;
                }

                var request = await _context.Requests.FirstOrDefaultAsync(r => r.RequestId == intake.LinkedRequestId, ct);
                if (request is null)
                {
                    _logger.LogWarning("Move skipped: request {RequestId} for intake {IntakeId} not found.",
                        intake.LinkedRequestId, intake.IntakeId);
                    return null;
                }

                if (string.IsNullOrEmpty(request.MailboxFolderId))
                {
                    var parentId = await _graph.EnsureFolderAsync(_options.RequestsParentFolder, null, ct);
                    if (string.IsNullOrEmpty(parentId))
                        return null;

                    request.MailboxFolderId =
                        await _graph.EnsureFolderAsync(RequestsIdentifierFactory.FolderName(request.Number), parentId, ct);
                    await _context.SaveChangesAsync(ct);
                }

                return request.MailboxFolderId;
            }

            case IntakeStatus.Discarded:
            {
                // File the discarded email into a folder under the Inbox (defaults to "General"), so it
                // drops out of the triage queue while staying in the mailbox where it can be found.
                // "inbox" is a well-known folder name Graph accepts wherever a folder id is expected,
                // so we can address its child folders directly without resolving the Inbox id first.
                return await _graph.EnsureFolderAsync(_options.DiscardFolder, "inbox", ct);
            }

            default:
                // Claimed / Failed etc. fall back to any configured static outcome folder.
                return OutcomeFolders.ResolveFolderId(status, _options.Folders);
        }
    }

    private async Task ReturnToInboxAsync(MailboxActionMessage action, CancellationToken ct)
    {
        if (!_options.EnableFolderMoves)
            return;

        var intake = await _context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == action.IntakeId, ct);
        if (intake is null)
        {
            _logger.LogWarning("Return-to-inbox skipped: intake {IntakeId} not found.", action.IntakeId);
            return;
        }

        if (string.IsNullOrEmpty(intake.GraphMessageId))
        {
            _logger.LogWarning("Return-to-inbox skipped: intake {IntakeId} has no Graph message id.", action.IntakeId);
            return;
        }

        // Guard against a same-folder move. If the discard/outcome move never actually filed the
        // email out of the Inbox, its id still points at an Inbox message — and Graph's /move would
        // DUPLICATE it (copy into the Inbox without removing the original). If it's already in the
        // Inbox there is nothing to return; leave the single copy in place.
        var inboxId = await _graph.GetFolderIdAsync("inbox", ct);
        var currentFolderId = await _graph.GetMessageParentFolderIdAsync(intake.GraphMessageId, ct);
        if (!string.IsNullOrEmpty(inboxId) && string.Equals(currentFolderId, inboxId, StringComparison.Ordinal))
        {
            _logger.LogInformation("Intake {IntakeId} is already in the Inbox; no return move needed.", action.IntakeId);
            return;
        }

        // "inbox" is a Graph well-known folder name accepted as a destination id.
        var newGraphId = await _graph.MoveMessageAsync(intake.GraphMessageId, "inbox", ct);
        intake.GraphMessageId = newGraphId;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Returned intake {IntakeId} to the Inbox for re-triage.", action.IntakeId);
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

    /// <summary>
    /// Renders the request's document (RFI etc.) from the SQL source of truth and emails it as a PDF
    /// attachment. Recipients are the project's flagged contacts, unless an ad-hoc override address is
    /// supplied (a resend to one person). The same render path serves the platform download, so the
    /// emailed PDF is byte-for-byte the file the triager can pull down. Best-effort: the queue retries.
    /// </summary>
    private async Task SendRequestDocumentAsync(MailboxActionMessage action, CancellationToken ct)
    {
        if (!_options.EnableRequestDocuments || string.IsNullOrEmpty(action.RequestId))
            return;

        var model = await RequestDocumentBuilder.BuildAsync(_context, action.RequestId, ct);
        if (model is null)
        {
            _logger.LogWarning("Request-document send skipped: request {RequestId} not found.", action.RequestId);
            return;
        }

        // Either the ad-hoc override (resend to one address) or the project's flagged contacts.
        var recipients = !string.IsNullOrWhiteSpace(action.RecipientOverride)
            ? new[] { new GraphRecipient(action.RecipientOverride.Trim()) }
            : model.Recipients.Select(r => new GraphRecipient(r.Email, r.Name)).ToArray();

        if (recipients.Length == 0)
        {
            _logger.LogWarning(
                "Request-document send skipped: request {RequestId} ({Number}) has no flagged contacts to email.",
                model.RequestId, model.DisplayNumber);
            return;
        }

        var pdf = RequestDocumentRenderer.Render(model);
        var attachment = new GraphAttachment(model.FileName, "application/pdf", pdf);
        var outbound = new GraphOutboundMessage(
            recipients, model.EmailSubject, BuildCoverNote(model), new[] { attachment });

        await _graph.SendMailAsync(outbound, ct);

        // Record the send on the request's shared activity history (the audit trail the document renders).
        var recipientList = string.Join(", ", recipients.Select(r => r.Email));
        _context.RequestMessages.Add(new RequestMessageEntity
        {
            MessageId = RequestsIdentifierFactory.Next(),
            RequestId = model.RequestId,
            AuthorEmail = _options.Mailbox,
            AuthorName = model.ProjectName,
            Body = $"{model.TypeShort} {model.DisplayNumber} document issued to {recipientList}.",
            Visibility = (int)MessageVisibility.Shared,
            PostedAt = DateTimeOffset.UtcNow,
            Direction = (int)MessageDirection.Outbound,
            SentStatus = (int)MessageSentStatus.Sent
        });

        // First issue moves an Open request to AwaitingResponse; a resend never regresses the status.
        var request = await _context.Requests.FirstOrDefaultAsync(r => r.RequestId == model.RequestId, ct);
        if (request is not null && request.Status == (int)RequestStatus.Open)
            request.Status = (int)RequestStatus.AwaitingResponse;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Issued {Type} {Number} to {Count} recipient(s).", model.TypeShort, model.DisplayNumber, recipients.Length);
    }

    /// <summary>The short branded HTML cover note that accompanies the attached document.</summary>
    private static string BuildCoverNote(RequestDocumentModel model)
    {
        var due = model.ResponseDue is { } d
            ? $"<p style=\"margin:0 0 12px\">A response is requested by <strong>{d:dd MMM yyyy}</strong>.</p>"
            : string.Empty;

        return $@"<div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5"">
  <p style=""margin:0 0 12px"">Please find attached <strong>{model.TypeShort} {model.DisplayNumber}</strong> &mdash; {System.Net.WebUtility.HtmlEncode(model.Title)} &mdash; for project {System.Net.WebUtility.HtmlEncode(model.ProjectName)} ({System.Net.WebUtility.HtmlEncode(model.ProjectReference)}).</p>
  {due}
  <p style=""margin:0 0 12px"">The attached PDF contains the full details and any references. Please reply to this email to respond.</p>
  <p style=""margin:16px 0 0;color:#C09A51;font-weight:bold"">Jewel Bespoke Build</p>
  <p style=""margin:0;color:#FF8300;font-size:12px"">jewelbb.co.uk</p>
</div>";
    }
}
