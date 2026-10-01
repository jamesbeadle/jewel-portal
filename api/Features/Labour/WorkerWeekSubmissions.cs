using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The submission rows as the worker and the office read them.</summary>
public static class WorkerWeekSubmissions
{
    public static Task<WorkerWeekSubmissionEntity?> FindAsync(
        JpmsContext context, string workerId, DateTimeOffset monday, CancellationToken cancellationToken) =>
        context.WorkerWeekSubmissions.FirstOrDefaultAsync(row => row.WorkerId == workerId && row.WeekStart == monday, cancellationToken);

    public static async Task<MyWeekSubmission?> MineAsync(
        JpmsContext context, string workerId, DateTimeOffset monday, CancellationToken cancellationToken)
    {
        var submission = await context.WorkerWeekSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.WorkerId == workerId && row.WeekStart == monday, cancellationToken);
        return submission is null ? null : ToMine(submission);
    }

    public static MyWeekSubmission ToMine(WorkerWeekSubmissionEntity submission) =>
        new((WorkerWeekSubmissionStatus)submission.Status, submission.SubmittedAt, submission.ReviewedByEmail, submission.ReviewedAt, submission.ReviewNote);

    public static WorkerWeekSubmission ToModel(WorkerWeekSubmissionEntity submission, string workerName, IReadOnlyList<MyWeekDay> days) =>
        new(submission.WorkerWeekSubmissionId, submission.WorkerId, workerName, submission.WeekStart,
            (WorkerWeekSubmissionStatus)submission.Status, submission.SubmittedAt, submission.ReviewedByEmail,
            submission.ReviewedAt, submission.ReviewNote, days);
}
