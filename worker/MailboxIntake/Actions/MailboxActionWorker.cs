using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Api.Features.Requests.Recipients;
using Jewel.JPMS.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Actions;

/// <summary>
/// Performs the one remaining queued mailbox side-effect out-of-band: rendering a request's document
/// (RFI etc.) and placing it as a pre-filled DRAFT in the mailbox for a human to review and send.
/// Nothing is ever sent from code. Best-effort and retried by the queue.
///
/// Folder moves and return-to-inbox are no longer done here — in the live-read model the triage
/// screen performs those moves synchronously in the API against the mailbox.
/// </summary>
public sealed class MailboxActionWorker
{
    private readonly JpmsContext _context;
    private readonly IGraphMailClient _graph;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<MailboxActionWorker> _logger;

    public MailboxActionWorker(
        JpmsContext context, IGraphMailClient graph,
        MailboxIntakeOptions options, ILogger<MailboxActionWorker> logger)
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
            case MailboxActionType.SendRequestDocument:
                await SendRequestDocumentAsync(action, ct);
                break;
            default:
                _logger.LogWarning("Ignoring unsupported mailbox action type {Type}.", action.Type);
                break;
        }
    }

    /// <summary>
    /// Renders the request's document (RFI etc.) from the SQL source of truth and creates a Drafts-
    /// folder email with it attached as a PDF — a human reviews and sends it from the mailbox.
    /// Recipients resolve through the shared RequestRecipientResolver (request party →
    /// project party → project profile, with the correspondence profile supplying CC/BCC), unless an
    /// ad-hoc override address is supplied (a resend to one person, nothing copied). The same render
    /// path serves the platform download, so the emailed PDF is byte-for-byte the file the triager
    /// can pull down. Best-effort: the queue retries.
    /// </summary>
    private async Task SendRequestDocumentAsync(MailboxActionMessage action, CancellationToken ct)
    {
        if (!_options.EnableRequestDocuments || string.IsNullOrEmpty(action.RequestId))
            return;

        var request = await _context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == action.RequestId, ct);
        if (request is null)
        {
            _logger.LogWarning("Request-document send skipped: request {RequestId} not found.", action.RequestId);
            return;
        }

        // EMAIL POLICY (defence in depth — the API gates this too): only RFI / NOD / EOT documents
        // are ever drafted for sending. A stray queue message for any other kind is dropped.
        var requestKind = (RequestType)request.Kind;
        if (!requestKind.IsEmailable())
        {
            _logger.LogWarning(
                "Request-document draft skipped: request {RequestId} is a {Kind}, which is never emailed.",
                action.RequestId, requestKind.DisplayName());
            return;
        }

        // Either the ad-hoc override (resend to one address, nothing copied) or the full resolved set.
        var recipientSet = !string.IsNullOrWhiteSpace(action.RecipientOverride)
            ? new RequestRecipientSet(
                new[] { new CorrespondenceRecipient("", action.RecipientOverride.Trim(), CorrespondenceRouting.To) },
                Array.Empty<CorrespondenceRecipient>(),
                Array.Empty<CorrespondenceRecipient>())
            : await RequestRecipientResolver.ResolveAsync(_context, request, ct);

        var model = await RequestDocumentBuilder.BuildAsync(_context, action.RequestId, ct, recipientSet);
        if (model is null)
        {
            _logger.LogWarning("Request-document send skipped: request {RequestId} not found.", action.RequestId);
            return;
        }

        if (!recipientSet.HasTo)
        {
            _logger.LogWarning(
                "Request-document send skipped: request {RequestId} ({Number}) has no correspondent to email — " +
                "link a party or set a project contact's routing to To.",
                model.RequestId, model.DisplayNumber);
            return;
        }

        static GraphRecipient AsGraph(CorrespondenceRecipient r) =>
            new(r.Email, string.IsNullOrWhiteSpace(r.Name) ? null : r.Name);

        var pdf = RequestDocumentRenderer.Render(model);
        var attachment = new GraphAttachment(model.FileName, "application/pdf", pdf);

        // The workflow tag rides on the draft and survives the send, so the Sent Items copy self-
        // associates with the record — and because this document opens a brand-new conversation,
        // replies to it inherit the tag through the thread sweep instead of waiting in triage.
        var recordTag = TriageCategories.ForRecord(await RequestTags.StemAsync(_context, request, ct));
        var outbound = new GraphOutboundMessage(
            recipientSet.To.Select(AsGraph).ToArray(),
            model.EmailSubject,
            BuildCoverNote(model),
            new[] { attachment },
            Cc: recipientSet.Cc.Select(AsGraph).ToArray(),
            Bcc: recipientSet.Bcc.Select(AsGraph).ToArray(),
            Categories: new[] { TriageCategories.Marker, recordTag });

        // HUMAN IN THE LOOP: the email is only ever placed in the mailbox's Drafts folder. A person
        // reviews it in Outlook and presses Send themselves — nothing is sent from code.
        var draft = await _graph.CreateDraftAsync(outbound, ct);
        if (draft is null)
        {
            _logger.LogWarning(
                "Request-document draft failed for {Type} {Number}; the queue will retry.",
                model.TypeShort, model.DisplayNumber);
            throw new InvalidOperationException(
                $"Graph draft create failed for request {model.RequestId}; retrying via the queue.");
        }

        // Record the drafted document on the request's shared activity history (shown on the request
        // page — the issued PDF carries no activity trail). This trail is client-facing, so it lists
        // To and CC only — BCC never appears on any shared surface (it is logged below as a count,
        // nothing more).
        var recipientList = string.Join(", ", recipientSet.To.Select(r => r.Email));
        if (recipientSet.Cc.Count > 0)
            recipientList += " (copied: " + string.Join(", ", recipientSet.Cc.Select(r => r.Email)) + ")";
        _context.RequestMessages.Add(new RequestMessageEntity
        {
            MessageId = RequestsIdentifierFactory.Next(),
            RequestId = model.RequestId,
            AuthorEmail = _options.Mailbox,
            AuthorName = model.ProjectName,
            Body = $"{model.TypeShort} {model.DisplayNumber} document drafted to {recipientList} — awaiting review and send from the mailbox.",
            Visibility = (int)MessageVisibility.Shared,
            PostedAt = DateTimeOffset.UtcNow,
            Direction = (int)MessageDirection.Outbound,
            SentStatus = (int)MessageSentStatus.Pending
        });

        // Drafted means it's going out: an Open request moves to Awaiting Response as soon as the
        // draft lands in the mailbox (matching the API's on-demand draft path). The team manually
        // sets it back to Open if the send is cancelled. Only Open moves — a request already
        // responded to, approved or closed keeps its status through a re-draft.
        if ((RequestStatus)request.Status == RequestStatus.Open)
            request.Status = (int)RequestStatus.AwaitingResponse;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Drafted {Type} {Number} for {ToCount} recipient(s), {CcCount} copied, {BccCount} blind-copied — awaiting human review in the mailbox Drafts folder.",
            model.TypeShort, model.DisplayNumber,
            recipientSet.To.Count, recipientSet.Cc.Count, recipientSet.Bcc.Count);
    }

    /// <summary>The short branded HTML cover note that accompanies the attached document.</summary>
    private static string BuildCoverNote(RequestDocumentModel model)
    {
        var due = model.ResponseDue is { } d
            ? $"<p style=\"margin:0 0 12px\">A response is requested by <strong>{d:dd MMM yyyy}</strong>.</p>"
            : string.Empty;

        // Lead with the client-visible reference (RFI-012) — matching the subject line, the PDF and
        // the register — not the internal REQ number. The type word is only added in the pre-numbering
        // fallback, where DisplayReference is the bare REQ number.
        var displayRef = !string.IsNullOrWhiteSpace(model.Reference)
            ? model.Reference
            : $"{model.TypeShort} {model.DisplayNumber}".Trim();

        return $@"<div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5"">
  <p style=""margin:0 0 12px"">Please find attached <strong>{displayRef}</strong> &mdash; {System.Net.WebUtility.HtmlEncode(model.Title)} &mdash; for project {System.Net.WebUtility.HtmlEncode(model.ProjectName)} ({System.Net.WebUtility.HtmlEncode(model.ProjectReference)}).</p>
  {due}
  <p style=""margin:0 0 12px"">The attached PDF contains the full details and any references. Please reply to this email to respond.</p>
  <p style=""margin:16px 0 0;color:#C09A51;font-weight:bold"">Jewel Bespoke Build</p>
  <p style=""margin:0;color:#FF8300;font-size:12px"">jewelbb.co.uk</p>
</div>";
    }
}
