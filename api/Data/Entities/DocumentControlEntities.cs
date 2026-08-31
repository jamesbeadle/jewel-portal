using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One email attachment sent to Document Control from the Control Centre: a point-in-time copy of
/// the file (bytes in the document-control blob store, keyed by BlobRef) plus a snapshot of the
/// email's envelope so the document keeps its context after the mailbox moves on. Filing copies the
/// bytes onward into the destination's own store and stamps the resolution columns; the row itself
/// never leaves this register (Discarded is restorable, never deleted). One row per
/// (MessageId, AttachmentId) is enforced by the send handler — the Graph ids are too long for a
/// unique SQL index (see JpmsContext).
/// </summary>
public sealed class DocumentControlItemEntity
{
    [Key, MaxLength(64)] public string DocumentControlItemId { get; set; } = "";

    // The source email. Graph ids stay usable to open the email while it exists; the envelope
    // snapshot below is what survives the mailbox.
    [MaxLength(512)]     public string MessageId { get; set; } = "";
    [MaxLength(512)]     public string? InternetMessageId { get; set; }
    [MaxLength(512)]     public string AttachmentId { get; set; } = "";
    [MaxLength(256)]     public string FromEmail { get; set; } = "";
    [MaxLength(256)]     public string FromName { get; set; } = "";
    [MaxLength(512)]     public string Subject { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }

    // The file.
    [MaxLength(256)]     public string FileName { get; set; } = "";
    [MaxLength(256)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";

    // The triage form's project at Apply time — a filing hint, not a rule.
    [MaxLength(64)]      public string? ProjectIdHint { get; set; }

    // DocumentControlStatus: 0 Pending, 1 Filed, 2 Discarded.
    public int Status { get; set; }
    [MaxLength(256)]     public string SentBy { get; set; } = "";
    public DateTimeOffset SentAt { get; set; }

    // Stamped when the item is filed or discarded.
    [MaxLength(256)]     public string? ResolvedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    // DocumentFiledAs: 0 Drawing, 1 PaymentCertificate, 2 SubcontractorDocument,
    // 3 ArchiveExtracted. Null until Filed.
    public int? FiledAsKind { get; set; }
    [MaxLength(64)]      public string? FiledRecordId { get; set; }
    // The Filed view's human sentence, e.g. "Drawing PRO-064-(WD)-P-800 Rev I on Ashtead House".
    [MaxLength(512)]     public string FiledLabel { get; set; } = "";
    // Provenance: the queue item this one was extracted from, when it arrived inside an archive.
    [MaxLength(64)]      public string? SourceDocumentControlItemId { get; set; }
}

/// <summary>
/// A payment certificate on a project — what the client is paying, certified. Keeps its own blob
/// copy (independent of any Document Control item it was filed from) so the register can never be
/// orphaned by queue housekeeping.
/// </summary>
public sealed class PaymentCertificateEntity
{
    [Key, MaxLength(64)] public string PaymentCertificateId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string CertificateNumber { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")]
    public decimal? CertifiedAmount { get; set; }
    public DateTimeOffset IssuedDate { get; set; }
    // The valuation claim this certificate certifies, when known.
    [MaxLength(64)]      public string? ValuationClaimId { get; set; }

    [MaxLength(256)]     public string FileName { get; set; } = "";
    [MaxLength(256)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string CreatedBy { get; set; } = "";
    // Provenance: the Document Control item this certificate was filed from, when it came that way.
    [MaxLength(64)]      public string? SourceDocumentControlItemId { get; set; }
}
