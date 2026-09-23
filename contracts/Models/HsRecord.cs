namespace Jewel.JPMS.Models;

public enum HsRecordKind
{
    Observation,
    NearMiss,
    Incident,
    CorrectiveAction,
    ToolboxTalk,
    Permit
}

public enum HsSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum HsStatus
{
    Open,
    InProgress,
    Closed
}

/// <summary>
/// One record on the H&amp;S register. CommentCount, LastCommentAt and HasPhoto are the thread's
/// standing as the register lists it (2026-09-23): what the site manager and the officer have said
/// on it, and whether a photograph of the work done is waiting for her.
/// </summary>
public sealed record HsRecord(
    string HsRecordId,
    string ProjectId,
    HsRecordKind Kind,
    string Summary,
    HsSeverity Severity,
    HsStatus Status,
    string AssignedToEmail,
    string AssignedToName,
    DateTimeOffset RaisedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? ClosedAt,
    int CommentCount = 0,
    DateTimeOffset? LastCommentAt = null,
    bool HasPhoto = false);

/// <summary>What was said on a record, by whom and when, with the photographs sent with it.</summary>
public sealed record HsRecordComment(
    string HsRecordCommentId,
    string HsRecordId,
    string AuthorEmail,
    string AuthorName,
    string Text,
    DateTimeOffset PostedAt,
    IReadOnlyList<HsRecordPhoto> Photos);

/// <summary>A photograph on a record's thread — the site manager's evidence that the work is done.</summary>
public sealed record HsRecordPhoto(
    string HsRecordPhotoId,
    string HsRecordId,
    string HsRecordCommentId,
    string FileName,
    string ContentType,
    DateTimeOffset UploadedAt);

public sealed record HsRecordAttendance(
    string HsRecordAttendanceId,
    string HsRecordId,
    string AttendeeName,
    string SignatureBlobRef,
    DateTimeOffset SignedAt);

public static class HsRecordExtensions
{
    public static string KindDisplayName(this HsRecordKind kind) => kind switch
    {
        HsRecordKind.Observation       => "Observation",
        HsRecordKind.NearMiss          => "Near miss",
        HsRecordKind.Incident          => "Incident",
        HsRecordKind.CorrectiveAction  => "Corrective action",
        HsRecordKind.ToolboxTalk       => "Toolbox talk",
        HsRecordKind.Permit            => "Permit",
        _ => kind.ToString()
    };

    /// <summary>Who owns the record as a person reads it: the name when one is on it, else the login.</summary>
    public static string OwnerDisplayName(this HsRecord record) =>
        string.IsNullOrWhiteSpace(record.AssignedToName) ? record.AssignedToEmail : record.AssignedToName;

    /// <summary>Who said something as the thread reads it: the name when one is on it, else the login.</summary>
    public static string AuthorDisplayName(this HsRecordComment comment) =>
        string.IsNullOrWhiteSpace(comment.AuthorName) ? comment.AuthorEmail : comment.AuthorName;

    public static bool IsCorrectiveAction(this HsRecord record) => record.Kind == HsRecordKind.CorrectiveAction;

    public static string DisplayName(this HsStatus status) => status switch
    {
        HsStatus.Open => "Open",
        HsStatus.InProgress => "In progress",
        HsStatus.Closed => "Closed",
        _ => status.ToString()
    };

    public static bool IsOpen(this HsRecord record) => record.Status != HsStatus.Closed;

    /// <summary>Open with a due date already past — the register's warning reading.</summary>
    public static bool IsOverdue(this HsRecord record) =>
        record.IsOpen() && record.DueAt is { } due && due < DateTimeOffset.UtcNow.Date;

}
