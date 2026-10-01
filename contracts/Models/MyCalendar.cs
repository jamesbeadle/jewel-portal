namespace Jewel.JPMS.Models;

/// <summary>Where a worker's submitted week has got to: with the office, signed off, or sent
/// back for the worker to amend and submit again.</summary>
public enum WorkerWeekSubmissionStatus
{
    Submitted = 0,
    SignedOff = 1,
    SentBack = 2,
}

/// <summary>The worker's own view of their week's submission: its state, and who answered it.</summary>
public sealed record MyWeekSubmission(
    WorkerWeekSubmissionStatus Status,
    DateTimeOffset SubmittedAt,
    string ReviewedByEmail = "",
    DateTimeOffset? ReviewedAt = null,
    string ReviewNote = "")
{
    public bool IsWithTheOffice => Status == WorkerWeekSubmissionStatus.Submitted;
    public bool IsSignedOff => Status == WorkerWeekSubmissionStatus.SignedOff;
    public bool IsSentBack => Status == WorkerWeekSubmissionStatus.SentBack;
    public bool IsLocked => IsWithTheOffice || IsSignedOff;
}

/// <summary>One week of the worker's days, Monday to Sunday, as My day pages through them: every
/// working day per site (a weekend day only when something was recorded on it), and the week's
/// submission once the worker has sent it to the office.</summary>
public sealed record MyLabourWeek(
    DateTimeOffset WeekStart,
    DateTimeOffset Today,
    IReadOnlyList<MyWeekDay> Days,
    MyWeekSubmission? Submission,
    bool CanBeSubmitted,
    string SubmitRefusal = "")
{
    public DateTimeOffset WeekEnd => WeekStart.AddDays(6);
    public bool IsLocked => Submission is { IsLocked: true };
}

/// <summary>The month's figures as the worker invoices from them — the same arithmetic as the
/// office's overview (hours to days at the standard day), so the invoice and the settlement agree.</summary>
public sealed record MyMonthTotals(
    decimal ApprovedHours,
    decimal ApprovedDays,
    int WaitingDays,
    int OffDays,
    int MissingDays)
{
    public int OutstandingDays => WaitingDays + MissingDays;
}

/// <summary>One calendar month of the worker's days with its totals — the month view on My day.</summary>
public sealed record MyLabourMonth(
    int Year,
    int Month,
    DateTimeOffset Today,
    IReadOnlyList<MyWeekDay> Days,
    MyMonthTotals Totals);
