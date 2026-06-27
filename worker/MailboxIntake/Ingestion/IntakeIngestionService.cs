using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;

/// <summary>
/// Turns a Graph message into a tracked <see cref="IntakeEmailEntity"/> exactly once.
/// Idempotency key is the email's InternetMessageId: if a row already exists we refresh the
/// (volatile) Graph id and skip; otherwise we insert a new NeedsTriage row. This is what stops
/// the webhook and the delta sweep from both creating a row for the same email. The unique index
/// on InternetMessageId is the database-level backstop behind this app-level check.
/// </summary>
public sealed class IntakeIngestionService
{
    private readonly JpmsContext _context;
    private readonly IMailboxActionScheduler _mailbox;
    private readonly ILogger<IntakeIngestionService> _logger;

    public IntakeIngestionService(JpmsContext context, IMailboxActionScheduler mailbox, ILogger<IntakeIngestionService> logger)
    {
        _context = context;
        _mailbox = mailbox;
        _logger = logger;
    }

    /// <summary>Returns true if a new row was inserted; false if it already existed or was skipped.</summary>
    public async Task<bool> IngestAsync(GraphMessage message, CancellationToken ct)
    {
        if (message.IsRemoved)
            return false; // a delete in the delta feed — nothing to ingest.

        if (string.IsNullOrEmpty(message.InternetMessageId))
        {
            _logger.LogWarning("Skipping mailbox message {GraphId} with no InternetMessageId.", message.Id);
            return false;
        }

        var existing = await _context.IntakeEmails
            .FirstOrDefaultAsync(e => e.InternetMessageId == message.InternetMessageId, ct);

        if (existing is not null)
        {
            // Keep the Graph id current (it changes whenever the message is moved between folders).
            if (!string.IsNullOrEmpty(message.Id) && existing.GraphMessageId != message.Id)
            {
                existing.GraphMessageId = message.Id;
                await _context.SaveChangesAsync(ct);
            }
            return false;
        }

        var conversationId = Clamp(message.ConversationId, 998);

        // Auto-link: if this email belongs to a thread that's already logged as a request, skip
        // triage entirely — attach it straight to that request and queue it to the request's folder.
        // Threads are matched on ConversationId only (the reliable Graph thread key).
        var linkedRequestId = await ResolveExistingRequestAsync(conversationId, ct);

        var entity = new IntakeEmailEntity
        {
            IntakeId = RequestsIdentifierFactory.Next(),
            InternetMessageId = message.InternetMessageId,
            GraphMessageId = message.Id,
            ConversationId = conversationId,
            InReplyTo = Clamp(message.InReplyTo, 998),
            ReferencesHeader = message.References,
            FromEmail = Clamp(message.FromEmail, 256) ?? "",
            FromName = Clamp(message.FromName, 256) ?? "",
            Subject = Clamp(message.Subject, 512) ?? "",
            BodyPreview = Clamp(message.BodyPreview, 4000) ?? "",
            HasAttachments = message.HasAttachments,
            ReceivedAt = message.ReceivedAt,
            Status = linkedRequestId is null ? (int)IntakeStatus.NeedsTriage : (int)IntakeStatus.Linked,
            LinkedRequestId = linkedRequestId
        };

        _context.IntakeEmails.Add(entity);

        if (linkedRequestId is not null)
            _context.RequestMessages.Add(IntakeConversation.AsInboundMessage(entity, linkedRequestId));

        try
        {
            await _context.SaveChangesAsync(ct);

            if (linkedRequestId is not null)
            {
                await _mailbox.ScheduleOutcomeMoveAsync(entity.IntakeId, IntakeStatus.Linked, ct);
                _logger.LogInformation(
                    "Auto-linked mailbox email {IntakeId} ({InternetMessageId}) to request {RequestId} on thread match.",
                    entity.IntakeId, entity.InternetMessageId, linkedRequestId);
            }
            else
            {
                _logger.LogInformation("Ingested mailbox email {IntakeId} ({InternetMessageId}).",
                    entity.IntakeId, entity.InternetMessageId);
            }
            return true;
        }
        catch (DbUpdateException ex)
        {
            // The unique index caught a race (webhook + sweep inserting concurrently). Safe to ignore.
            _context.Entry(entity).State = EntityState.Detached;
            _logger.LogInformation(ex,
                "Duplicate intake suppressed by unique index for {InternetMessageId}.", message.InternetMessageId);
            return false;
        }
    }

    /// <summary>
    /// Finds the request, if any, that this thread already belongs to. Matches on ConversationId
    /// against the conversation entries we recorded for previously-triaged emails, and only returns
    /// a request that still exists (a returned-to-triage / deleted request must not auto-capture).
    /// </summary>
    private async Task<string?> ResolveExistingRequestAsync(string? conversationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return null;

        return await _context.RequestMessages
            .Where(m => m.ConversationId == conversationId)
            .Join(_context.Requests, m => m.RequestId, r => r.RequestId, (m, r) => r.RequestId)
            .FirstOrDefaultAsync(ct);
    }

    private static string? Clamp(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
