using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// Signing a submitted week off is the office's whole answer in one act: every waiting day is
/// approved through the one approval door (the worker's rate on the day, the budget hard-block),
/// a day the worker recorded off becomes a recorded absence so the month-end stops chasing it,
/// the week's sign-off marker is written for each month it touches, and the row records who signed
/// and when. A day the approval door refuses — over budget, uncoded — refuses the whole sign-off
/// by name, because a half-signed week is worse than a named day to sort on the Labour tab.
/// </summary>
public sealed class SignOffWorkerWeekHandler : ICommandHandler<SignOffWorkerWeek, WorkerWeekSubmission>
{
    private readonly JpmsContext context;
    private readonly ApproveTimesheetsHandler approvals;
    private readonly SignOffLabourWeekHandler markers;
    private readonly WorkerWeekSubmissionReader reader;

    public SignOffWorkerWeekHandler(JpmsContext context, ApproveTimesheetsHandler approvals, SignOffLabourWeekHandler markers, WorkerWeekSubmissionReader reader)
    { this.context = context; this.approvals = approvals; this.markers = markers; this.reader = reader; }

    public Task<WorkerWeekSubmission> HandleAsync(SignOffWorkerWeek command, CancellationToken cancellationToken) =>
        HandleAsync(command, signedOffByEmail: "", cancellationToken);

    public async Task<WorkerWeekSubmission> HandleAsync(SignOffWorkerWeek command, string signedOffByEmail, CancellationToken cancellationToken)
    {
        var submission = await SubmittedWeeks.AwaitingReviewAsync(context, command.WorkerWeekSubmissionId, cancellationToken);
        var worker = await context.Workers.FirstAsync(row => row.WorkerId == submission.WorkerId, cancellationToken);
        var week = await reader.ReadAsync(submission, worker, cancellationToken);
        RefuseADaySentBack(week);
        await ApproveWaitingDaysAsync(week, signedOffByEmail, cancellationToken);
        await RecordOffDaysAsAbsencesAsync(week, worker, signedOffByEmail, cancellationToken);
        await WriteMarkersAsync(week, signedOffByEmail, cancellationToken);

        submission.Status = (int)WorkerWeekSubmissionStatus.SignedOff;
        submission.ReviewedByEmail = signedOffByEmail;
        submission.ReviewedAt = DateTimeOffset.UtcNow;
        submission.ReviewNote = "";
        await context.SaveChangesAsync(cancellationToken);
        return await reader.ReadAsync(submission, worker, cancellationToken);
    }

    private static void RefuseADaySentBack(WorkerWeekSubmission week)
    {
        var sentBack = week.Days.FirstOrDefault(day => day.Status == TimesheetStatus.Rejected);
        if (sentBack is null) return;
        throw new InvalidOperationException($"{sentBack.Date:dddd d MMMM} was sent back to the operative — send the week back so they can amend it.");
    }

    private async Task ApproveWaitingDaysAsync(WorkerWeekSubmission week, string signedOffByEmail, CancellationToken cancellationToken)
    {
        var waitingBySite = week.Days
            .Where(day => day.Status == TimesheetStatus.Submitted)
            .GroupBy(day => day.ProjectId);
        var refusals = new List<string>();
        foreach (var site in waitingBySite)
        {
            var ids = site.Select(day => day.TimesheetId).ToList();
            var outcome = await approvals.HandleAsync(new ApproveTimesheets(site.Key, ids), signedOffByEmail, cancellationToken);
            refusals.AddRange(outcome.Failures.Select(failure => Reason(site, failure)));
        }
        if (refusals.Count > 0) throw new InvalidOperationException(string.Join(" ", refusals));
    }

    private static string Reason(IGrouping<string, MyWeekDay> site, LabourApprovalFailure failure)
    {
        var day = site.First(row => row.TimesheetId == failure.TimesheetId);
        return $"{day.Date:ddd d MMM} on {day.ProjectName}: {failure.Reason}";
    }

    private async Task RecordOffDaysAsAbsencesAsync(WorkerWeekSubmission week, WorkerEntity worker, string signedOffByEmail, CancellationToken cancellationToken)
    {
        var offDays = week.Days.Where(day => day.Kind == MyWeekDayKind.Off && !day.IsPlannedOff).GroupBy(day => day.Date);
        foreach (var offDay in offDays)
        {
            var words = offDay.First().Words;
            context.WorkerAbsences.Add(new WorkerAbsenceEntity
            {
                WorkerAbsenceId = LabourIdentifierFactory.NextWorkerAbsenceId(),
                WorkerId = worker.WorkerId,
                Date = offDay.Key,
                Kind = (int)AbsenceKind.NotWorked,
                Note = words,
                RecordedByEmail = signedOffByEmail,
                RecordedAt = DateTimeOffset.UtcNow,
            });
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task WriteMarkersAsync(WorkerWeekSubmission week, string signedOffByEmail, CancellationToken cancellationToken)
    {
        var months = LabourWeeks.Between(week.WeekStart, week.WeekStart.AddDays(LabourWeeks.DaysInWeek - 1))
            .Select(LabourWeeks.MonthStartOf)
            .Distinct();
        foreach (var month in months)
            await markers.HandleAsync(new SignOffLabourWeek(week.WorkerId, week.WeekStart, month), signedOffByEmail, cancellationToken);
    }
}
