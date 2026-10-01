using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>A week the worker has sent to the office is closed to them until it is answered: while
/// it waits nothing in it may be amended or added, and once signed off it is the office's. A week
/// sent back is open again. Every door that writes a worker's day asks here first.</summary>
public static class WorkerWeekLock
{
    private const string WithTheOffice = "This week is with the office for review — it reopens if it is sent back.";
    private const string SignedOff = "The office has signed this week off — ask your Project Manager to change anything in it.";

    public static async Task EnsureOpenAsync(JpmsContext context, string workerId, DateTimeOffset workDate, CancellationToken cancellationToken)
    {
        var monday = LabourWeeks.MondayOf(workDate);
        var submission = await context.WorkerWeekSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.WorkerId == workerId && row.WeekStart == monday, cancellationToken);
        if (submission is null) return;
        var status = (WorkerWeekSubmissionStatus)submission.Status;
        if (status == WorkerWeekSubmissionStatus.Submitted) throw new InvalidOperationException(WithTheOffice);
        if (status == WorkerWeekSubmissionStatus.SignedOff) throw new InvalidOperationException(SignedOff);
    }

    public static async Task<HashSet<DateTimeOffset>> LockedWeeksAsync(
        JpmsContext context, string workerId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var firstMonday = LabourWeeks.MondayOf(from);
        var locked = await context.WorkerWeekSubmissions.AsNoTracking()
            .Where(row => row.WorkerId == workerId && row.WeekStart >= firstMonday && row.WeekStart <= to)
            .Where(row => row.Status != (int)WorkerWeekSubmissionStatus.SentBack)
            .Select(row => row.WeekStart)
            .ToListAsync(cancellationToken);
        return locked.ToHashSet();
    }

    public static bool IsLocked(WorkerWeekSubmissionEntity? submission) =>
        submission is not null && (WorkerWeekSubmissionStatus)submission.Status != WorkerWeekSubmissionStatus.SentBack;
}
