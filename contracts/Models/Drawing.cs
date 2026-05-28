namespace Jewel.JPMS.Models;

public sealed record Drawing(
    string DrawingId,
    string ProjectId,
    string DrawingCode,
    string Title,
    string CurrentRevision,
    DateTimeOffset CreatedAt);

public sealed record DrawingRevision(
    string DrawingRevisionId,
    string DrawingId,
    string RevisionLabel,
    string FileName,
    string IssuedByEmail,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? SupersededAt,
    bool IsAmbiguous,
    int ViewCount);

public sealed record DrawingIssueRecord(
    string DrawingIssueRecordId,
    string DrawingRevisionId,
    string Source,
    string IssuedByName,
    DateTimeOffset IssuedAt,
    string Notes);
