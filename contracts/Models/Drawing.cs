namespace Jewel.JPMS.Models;

public sealed record Drawing(
    string DrawingId,
    string ProjectId,
    string DrawingCode,
    string Title,
    string? CurrentApprovedRevisionLabel,
    DateTimeOffset CreatedAt,
    int UnapprovedCount = 0,
    int ArchivedCount = 0,
    // Pipeline status of the LATEST revision (Bluebeam metadata extraction / change analysis),
    // rolled up so the drawing register can flag un-extracted or un-analysed drawings at a glance.
    DateTimeOffset? LatestMetadataExtractedAt = null,
    DateTimeOffset? LatestAnalysedAt = null);

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
    DateTimeOffset? ApprovedAt,
    // Pipeline status stamps — null until that stage has run for this revision.
    DateTimeOffset? MetadataExtractedAt = null,
    DateTimeOffset? AnalysedAt = null);

public sealed record DrawingIssueRecord(
    string DrawingIssueRecordId,
    string DrawingRevisionId,
    string Source,
    string IssuedByName,
    DateTimeOffset IssuedAt,
    string Notes);
