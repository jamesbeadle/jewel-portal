using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// Append-only audit trail of client-facing interactions (see Jewel.JPMS.Models.AuditEvent and
// docs/Pathway-Split-Platform-Flow-Plan.md §4). Generalises the ValuationInvoiceEventEntity
// pattern across features: cross-cutting communication/triage events land here; deep domain
// lifecycles (valuation invoices) keep their own specialised logs. Rows are never updated or
// deleted. Graph ids are long and opaque, hence the generous column widths; InternetMessageId is
// the stable key for re-finding a message in Outlook, WebLink opens it in Outlook on the web.
public sealed class AuditEventEntity
{
    [Key, MaxLength(64)]  public string AuditEventId { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    [MaxLength(256)]      public string ActorEmail { get; set; } = "";
    public int EventType { get; set; }
    [MaxLength(32)]       public string Pathway { get; set; } = "";
    [MaxLength(64)]       public string? ProjectId { get; set; }
    public int? RecordType { get; set; }
    [MaxLength(64)]       public string? RecordId { get; set; }
    [MaxLength(64)]       public string RecordReference { get; set; } = "";
    [MaxLength(512)]      public string? ConversationId { get; set; }
    [MaxLength(512)]      public string? EmailMessageId { get; set; }
    [MaxLength(512)]      public string? InternetMessageId { get; set; }
    [MaxLength(1024)]     public string? WebLink { get; set; }
    [MaxLength(1024)]     public string Detail { get; set; } = "";
}
