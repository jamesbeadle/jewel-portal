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

    public static string SeverityPillClass(this HsSeverity severity) => severity switch
    {
        HsSeverity.Low      => "bg-slate-100 border-slate-200 text-slate-700",
        HsSeverity.Medium   => "bg-amber-50 border-amber-200 text-amber-800",
        HsSeverity.High     => "bg-rose-50 border-rose-200 text-rose-800",
        HsSeverity.Critical => "bg-rose-100 border-rose-300 text-rose-900",
        _ => "bg-slate-100 border-slate-200 text-slate-700"
    };
}
