using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Audit;

/// <summary>
/// Appends one row to the client-facing audit trail (docs/Pathway-Split-Platform-Flow-Plan.md §4).
/// Generalises the ValuationInvoiceAuditTrail pattern across features. WriteAsync saves immediately
/// in its own call and is best-effort by design: an audit failure must never fail the action it
/// records (the mailbox tag write is the primary truth; the trail is the index over it), so
/// exceptions are logged and swallowed. Callers therefore invoke it AFTER the action has succeeded.
/// </summary>
public sealed class AuditTrail
{
    private readonly JpmsContext context;
    private readonly AuditActor actor;
    private readonly ILogger<AuditTrail> logger;

    public AuditTrail(JpmsContext context, AuditActor actor, ILogger<AuditTrail> logger)
    {
        this.context = context;
        this.actor = actor;
        this.logger = logger;
    }

    /// <summary>Short pathway label ("Client") from a bucket category ("JPMS/Client"); "" for null.</summary>
    public static string PathwayLabel(string? bucketCategory) =>
        string.IsNullOrEmpty(bucketCategory) ? ""
        : bucketCategory.Equals(TriageCategories.Client, StringComparison.OrdinalIgnoreCase) ? "Client"
        : bucketCategory.Equals(TriageCategories.Subcontractor, StringComparison.OrdinalIgnoreCase) ? "Subcontractor"
        : bucketCategory.Equals(TriageCategories.Internal, StringComparison.OrdinalIgnoreCase) ? "Internal"
        : bucketCategory;

    public async Task WriteAsync(
        AuditEventType eventType,
        string detail,
        string pathway = "",
        string? projectId = null,
        RecordType? recordType = null,
        string? recordId = null,
        string recordReference = "",
        string? conversationId = null,
        string? emailMessageId = null,
        string? internetMessageId = null,
        string? webLink = null,
        string? actorEmail = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            context.AuditEvents.Add(new AuditEventEntity
            {
                AuditEventId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTimeOffset.UtcNow,
                ActorEmail = Clamp(actorEmail ?? actor.Email, 256) ?? "",
                EventType = (int)eventType,
                Pathway = Clamp(pathway, 32) ?? "",
                ProjectId = projectId,
                RecordType = recordType is { } rt ? (int)rt : null,
                RecordId = recordId,
                RecordReference = Clamp(recordReference, 64) ?? "",
                ConversationId = Clamp(conversationId, 512),
                EmailMessageId = Clamp(emailMessageId, 512),
                InternetMessageId = Clamp(internetMessageId, 512),
                WebLink = Clamp(webLink, 1024),
                Detail = Clamp(detail, 1024) ?? ""
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Audit write failed for {EventType}: {Detail}", eventType, detail);
        }
    }

    private static string? Clamp(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
