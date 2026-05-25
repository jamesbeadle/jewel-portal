namespace Jewel.JPMS.Models;

public enum ChangeKind
{
    Rfi,
    Submittal,
    Variation,
    NoticeOfDelay
}

public enum ChangeStatus
{
    Open,
    AwaitingResponse,
    Approved,
    Rejected,
    Closed
}

public sealed record ChangeRecord(
    string ChangeRecordId,
    string ProjectId,
    ChangeKind Kind,
    string Reference,
    string Title,
    string Description,
    ChangeStatus Status,
    decimal? Value,
    string RaisedByEmail,
    DateTimeOffset RaisedAt,
    DateTimeOffset? RespondedAt);

public static class ChangeKindExtensions
{
    public static string DisplayName(this ChangeKind kind) => kind switch
    {
        ChangeKind.Rfi           => "RFI",
        ChangeKind.Submittal     => "Submittal",
        ChangeKind.Variation     => "Variation",
        ChangeKind.NoticeOfDelay => "Notice of Delay",
        _ => kind.ToString()
    };
}
