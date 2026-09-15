namespace Jewel.JPMS.Models;

// The spreadsheet's "Item Rate Key": the score points an item earns. Persisted as the points.
public enum HsAuditRate
{
    NotInPlace = 0,
    OneWeekOutOfDate = 5,
    UpToDate = 10
}

// The "Classification (Class)" key. Persisted as its integer value; the letter is display only.
public enum HsAuditClass
{
    A = 0,
    B = 1,
    C = 2,
    D = 3,
    E = 4
}

// The "Time Scale Key" — how soon a finding must be put right.
public enum HsAuditTimeScale
{
    Immediately = 0,
    WithinOneDay = 1,
    WithinThreeDays = 2,
    WithinSevenDays = 3,
    WithinOneMonth = 4,
    Ongoing = 5
}

// The "Comment Key" — why an item carries no rating, or that it is a repeat finding.
public enum HsAuditComment
{
    NotApplicable = 0,
    Note = 1,
    NotChecked = 2,
    NotSeen = 3,
    Repeat = 4
}

/// <summary>
/// One line of the framework on one audit — the template item with what the officer found.
/// Everything the spreadsheet row holds: comment code, rate, class, hand-entered minus,
/// time-scale, findings text, the owner's name (a person, not a login) and the date rectified.
/// HsRecordId is the corrective action Issue minted for it, when it minted one.
/// </summary>
public sealed record HsAuditItem(
    string HsAuditItemId,
    string HsAuditId,
    string Code,
    int Section,
    string Name,
    HsAuditComment? Comment,
    HsAuditRate? Rate,
    HsAuditClass? Class,
    int Minus,
    HsAuditTimeScale? TimeScale,
    string Findings,
    string OwnerName,
    DateTimeOffset? DateRectified,
    string? HsRecordId);
