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
/// Performs the one remaining queued mailbox side-effect out-of-band: rendering a request's document
/// (RFI etc.) and emailing it to the project's contacts. Best-effort and retried by the queue.
///
/// Folder moves and return-to-inbox are no longer done here — in the live-read model the triage
/// screen performs those moves synchronously in the API against the mailbox.
/// </summary>
public sealed class MailboxActionWorker
{
    private readonly JpmsContext _context;
    private readonly IGraphMailClient _graph;
    private readonly RequestEmailReader _emails;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<MailboxActionWorker> _logger;

    public MailboxActionWorker(
        JpmsContext context, IGraphMailClient graph, RequestEmailReader emails,
        MailboxIntakeOptions options, ILogger<MailboxActionWorker> logger)
    {
        _context = context;
        _graph = graph;
        _emails = emails;
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
            case MailboxActionType.SendRequestDocument:
                await SendRequestDocumentAsync(action, ct);
                break;
            default:
                _logger.LogWarning("Ignoring unsupported mailbox action type {Type}.", action.Type);
                break;
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

        var tagged = await _emails.ForRequestAsync(action.RequestId, ct);
        var model = await RequestDocumentBuilder.BuildAsync(_context, action.RequestId, tagged, ct);
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
