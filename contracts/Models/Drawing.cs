namespace Jewel.JPMS.Models;

public sealed record Drawing(
    string DrawingId,
    string ProjectId,
    string DrawingCode,
    string Title,
    string? CurrentApprovedRevisionLabel,
    DateTimeOffset CreatedAt,
    int UnapprovedCount = 0,
    int ArchivedCount = 0);

public sealed record DrawingRevision(
    string DrawingRevisionId,
    string DrawingId,
    string RevisionLabel,
    string FileName,
    string IssuedByEmail,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? SupersededAt,
    bool IsAmbiguous,
    int ViewCount,
    DrawingApprovalStatus ApprovalStatus,
    string? BlobRef,
    string? ContentType,
    long? FileSizeBytes,
    string? ApprovedByEmail,
    DateTimeOffset? ApprovedAt);

public sealed record DrawingIssueRecord(
    string DrawingIssueRecordId,
    string DrawingRevisionId,
    string Source,
    string IssuedByName,
    DateTimeOffset IssuedAt,
    string Notes);
