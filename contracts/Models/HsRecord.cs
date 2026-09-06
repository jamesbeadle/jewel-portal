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

public sealed record HsRecord(
    string HsRecordId,
    string ProjectId,
    HsRecordKind Kind,
    string Summary,
    HsSeverity Severity,
    HsStatus Status,
    string AssignedToEmail,
    DateTimeOffset RaisedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? ClosedAt);

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

}
