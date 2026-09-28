namespace Jewel.JPMS.Contracts.Progress;

/// <summary>What a day's filing is: a worker's daily log, a worker's recorded day off, or a note
/// written from the office or the Progress tab.</summary>
public enum ContractorsReportFilingKind { DailyLog, OffDay, Note }

/// <summary>One filing on one day of the reporting week: whose it is, what kind, how many
/// photographs it carries and whether the report has it selected.</summary>
public sealed record ContractorsReportFiling(
    string ProgressUpdateId,
    string FiledBy,
    ContractorsReportFilingKind Kind,
    int PhotoCount,
    bool IsSelected);

/// <summary>
/// One day of the reporting week as the composer lists it (Jeremy, 21 Sep 2026: the chase is the
/// composer's list on the Friday — no notification service): who filed, who signed in on site and
/// filed nothing, and how many photographs the day holds. A recorded day off is a filing, so a
/// day off reads whole, never missing; a working day with nothing at all is what the person
/// composing goes and asks about.
/// </summary>
public sealed record ContractorsReportWeekDay(
    DateOnly Date,
    IReadOnlyList<ContractorsReportFiling> Filed,
    IReadOnlyList<string> OnSiteNotFiled)
{
    public bool IsWorkingDay => Date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
    public int PhotoCount => Filed.Sum(filing => filing.PhotoCount);
    public bool HasNothingRecorded => Filed.Count == 0 && OnSiteNotFiled.Count == 0;
    public bool IsWhole => Filed.Count > 0 && OnSiteNotFiled.Count == 0;
    public bool NeedsAsking => IsWorkingDay && !IsWhole;
}
