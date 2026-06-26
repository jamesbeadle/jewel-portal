using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

// Persisted form of an inbound projects@ mailbox email awaiting / completing triage.
// InternetMessageId is the natural dedupe key, enforced by a unique index (the database-level
// backstop behind the ingestion layer's app-level idempotency check). ReferencesHeader can be
// long, so it is left unbounded (nvarchar(max)). GraphMessageId is the mailbox item's current
// Graph id, used to move the email into an outcome folder during triage; it is refreshed
// whenever the message is moved.
[Index(nameof(InternetMessageId), IsUnique = true)]
public sealed class IntakeEmailEntity
{
    [Key, MaxLength(64)] public string IntakeId { get; set; } = "";
    [MaxLength(450)]     public string InternetMessageId { get; set; } = "";
    [MaxLength(450)]     public string? GraphMessageId { get; set; }
    [MaxLength(998)]     public string? ConversationId { get; set; }
    [MaxLength(998)]     public string? InReplyTo { get; set; }
                         public string? ReferencesHeader { get; set; }
    [MaxLength(256)]     public string FromEmail { get; set; } = "";
    [MaxLength(256)]     public string FromName { get; set; } = "";
    [MaxLength(512)]     public string Subject { get; set; } = "";
    [MaxLength(4000)]    public string BodyPreview { get; set; } = "";
    public bool HasAttachments { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public int Status { get; set; }
    [MaxLength(256)]     public string? ClaimedByEmail { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    [MaxLength(64)]      public string? LinkedRequestId { get; set; }
    [MaxLength(512)]     public string? Notes { get; set; }
}
