namespace Jewel.JPMS.Models;

/// <summary>Where a Document Control item is in its own triage: waiting in the queue, filed to a
/// destination record, or discarded (kept, restorable — never deleted).</summary>
public enum DocumentControlStatus
{
    Pending = 0,
    Filed = 1,
    Discarded = 2
}

/// <summary>The destination a filed Document Control item landed in.</summary>
public enum DocumentFiledAs
{
    Drawing = 0,
    PaymentCertificate = 1,
    SubcontractorDocument = 2
}

/// <summary>
/// One email attachment sent to Document Control from the Control Centre: a point-in-time copy of
/// the file (the bytes live in the document-control blob store) plus a snapshot of the email's
/// envelope, so the document keeps its context even after the mailbox moves on. Filing copies the
/// bytes onward into the destination's own store — the item itself never leaves this register.
/// </summary>
public sealed record DocumentControlItem(
    string DocumentControlItemId,
    // The source email. MessageId/AttachmentId are Graph ids (still usable to open the email while
    // it exists); the envelope fields are the snapshot that survives the mailbox.
    string MessageId,
    string? InternetMessageId,
    string AttachmentId,
    string FromEmail,
    string FromName,
    string Subject,
    DateTimeOffset ReceivedAt,
    // The file.
    string FileName,
    string ContentType,
    long FileSizeBytes,
    // The project the triager had picked when the send was staged — a hint for filing, not a rule.
    string? ProjectIdHint,
    DocumentControlStatus Status,
    string SentBy,
    DateTimeOffset SentAt,
    // Stamped when the item is filed or discarded.
    string? ResolvedBy,
    DateTimeOffset? ResolvedAt,
    DocumentFiledAs? FiledAs,
    string? FiledRecordId,
    // The Filed view's human sentence, e.g. "Drawing PRO-064-(WD)-P-800 Rev I on Ashtead House".
    string FiledLabel = "");

/// <summary>
/// A payment certificate — the client's (or their agent's) certificate saying what is being paid
/// against a valuation. Stored under Finance, viewable by project; the file keeps its own blob
/// copy so the register never depends on the Document Control queue item it came from.
/// </summary>
public sealed record PaymentCertificate(
    string PaymentCertificateId,
    string ProjectId,
    string CertificateNumber,
    decimal? CertifiedAmount,
    DateTimeOffset IssuedDate,
    // The valuation claim this certificate certifies, when known (ValuationClaim.DisplayName).
    string? ValuationClaimId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    // Provenance: the Document Control item this certificate was filed from, when it came that way.
    string? SourceDocumentControlItemId);
