using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Hs.Audits;

/// <summary>
/// What Issue mints onto the project's H&S register: one corrective action per finding. A
/// finding is an item with an owner named or a rate below full marks, unless the officer marked
/// it N/A — derived from the row's own facts, not from the time-scale, because the officer's own
/// archive (which keyed off time-scale alone) captured nothing from an audit with no time-scales
/// filled in. Severity follows the class; the due date follows the time-scale from the issue date.
/// Pure: the planner writes nothing.
/// </summary>
internal static class HsAuditCorrectiveActions
{
    private const int SummaryLength = 512;
    private const int DaysInAMonth = 30;

    public static bool IsFinding(HsAuditItemEntity item)
    {
        if (item.Comment == (int)HsAuditComment.NotApplicable) return false;
        var hasOwner = !string.IsNullOrWhiteSpace(item.OwnerName);
        var isBelowFullMarks = item.Rate is { } rate && rate < (int)HsAuditRate.UpToDate;
        return hasOwner || isBelowFullMarks;
    }

    public static bool IsUnlinkedFinding(HsAuditItemEntity item) => item.HsRecordId is null && IsFinding(item);

    public static HsRecordEntity Mint(HsAuditItemEntity item, string projectId, DateTimeOffset issuedAt) => new()
    {
        HsRecordId = HsIdentifierFactory.NextHsRecordId(),
        ProjectId = projectId,
        Kind = (int)HsRecordKind.CorrectiveAction,
        Summary = SummaryOf(item),
        Severity = (int)SeverityOf(item),
        Status = (int)HsStatus.Open,
        AssignedToEmail = "",
        AssignedToName = item.OwnerName.Trim(),
        RaisedAt = issuedAt,
        DueAt = DueDateOf(item, issuedAt),
        ClosedAt = null
    };

    public static string SummaryOf(HsAuditItemEntity item)
    {
        var heading = $"{item.Code} {item.Name}";
        var summary = string.IsNullOrWhiteSpace(item.Findings) ? heading : $"{heading} — {item.Findings.Trim()}";
        return summary.Length <= SummaryLength ? summary : summary[..SummaryLength];
    }

    public static HsSeverity SeverityOf(HsAuditItemEntity item) => item.Class switch
    {
        (int)HsAuditClass.A => HsSeverity.Critical,
        (int)HsAuditClass.B => HsSeverity.High,
        (int)HsAuditClass.C => HsSeverity.Medium,
        _ => HsSeverity.Low
    };

    public static DateTimeOffset? DueDateOf(HsAuditItemEntity item, DateTimeOffset issuedAt)
    {
        var issuedOn = HsAuditRules.AsCalendarDate(issuedAt);
        return item.TimeScale switch
        {
            (int)HsAuditTimeScale.Immediately => issuedOn,
            (int)HsAuditTimeScale.WithinOneDay => issuedOn.AddDays(1),
            (int)HsAuditTimeScale.WithinThreeDays => issuedOn.AddDays(3),
            (int)HsAuditTimeScale.WithinSevenDays => issuedOn.AddDays(7),
            (int)HsAuditTimeScale.WithinOneMonth => issuedOn.AddDays(DaysInAMonth),
            _ => null
        };
    }
}
