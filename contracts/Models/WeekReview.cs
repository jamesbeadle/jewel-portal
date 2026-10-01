namespace Jewel.JPMS.Models;

/// <summary>A worker's week as submitted for review: who, which week, where it has got to, and
/// the days in it as the worker filed them — what a director reads before signing it off.</summary>
public sealed record WorkerWeekSubmission(
    string WorkerWeekSubmissionId,
    string WorkerId,
    string WorkerName,
    DateTimeOffset WeekStart,
    WorkerWeekSubmissionStatus Status,
    DateTimeOffset SubmittedAt,
    string ReviewedByEmail,
    DateTimeOffset? ReviewedAt,
    string ReviewNote,
    IReadOnlyList<MyWeekDay> Days)
{
    public bool IsAwaitingReview => Status == WorkerWeekSubmissionStatus.Submitted;
    public decimal TotalHours => Days.Where(day => day.IsLogged).Sum(day => day.Hours);
}
