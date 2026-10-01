using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.MyDayWeeksSeed;

namespace Jewel.JPMS.Tests;

// A director answers a submitted week in one step (1 Oct 2026): sign it off — every day approved,
// the off day a recorded absence, the month-end marker written, the week locked with who and when —
// or send it back with a note so the operative can amend and submit again.
public sealed class WeekReviewTests
{
    [Fact]
    public async Task SigningOff_approvesEveryDay_recordsTheOffDay_writesTheMarker_andLocksTheWeek()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1), LastMonday.AddDays(2), LastMonday.AddDays(3));
        await RecordOffAsync(context, LastMonday.AddDays(4));
        await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);
        var submission = await context.WorkerWeekSubmissions.SingleAsync();

        var signedOff = await SignOffHandler(context).HandleAsync(new SignOffWorkerWeek(submission.WorkerWeekSubmissionId), Director, CancellationToken.None);

        Assert.Equal(WorkerWeekSubmissionStatus.SignedOff, signedOff.Status);
        Assert.Equal(Director, signedOff.ReviewedByEmail);
        Assert.NotNull(signedOff.ReviewedAt);
        Assert.All(await context.Timesheets.ToListAsync(), row => Assert.Equal((int)TimesheetStatus.Approved, row.Status));
        Assert.All(await context.Timesheets.ToListAsync(), row => Assert.Equal(200m, row.CostAmount));
        var absence = await context.WorkerAbsences.SingleAsync();
        Assert.Equal(LastMonday.AddDays(4), absence.Date);
        Assert.Equal("Rained off.", absence.Note);
        Assert.NotEmpty(await context.LabourWeekSignOffs.Where(row => row.WeekStart == LastMonday).ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WorkerWeekLock.EnsureOpenAsync(context, WorkerId, LastMonday, CancellationToken.None));
        var mine = await WeekHandler(context).HandleAsync(new GetMyLabourWeek(LastMonday), Email, CancellationToken.None);
        Assert.True(mine.Submission is { IsSignedOff: true });
    }

    [Fact]
    public async Task SendingBack_needsANote_reopensTheWeek_andLetsItBeSubmittedAgain()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1), LastMonday.AddDays(2), LastMonday.AddDays(3), LastMonday.AddDays(4));
        await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);
        var submission = await context.WorkerWeekSubmissions.SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SendBackHandler(context).HandleAsync(new SendBackWorkerWeek(submission.WorkerWeekSubmissionId, " "), Director, CancellationToken.None));

        var sentBack = await SendBackHandler(context).HandleAsync(
            new SendBackWorkerWeek(submission.WorkerWeekSubmissionId, "Tuesday was a half day."), Director, CancellationToken.None);

        Assert.Equal(WorkerWeekSubmissionStatus.SentBack, sentBack.Status);
        Assert.Equal("Tuesday was a half day.", sentBack.ReviewNote);
        await WorkerWeekLock.EnsureOpenAsync(context, WorkerId, LastMonday, CancellationToken.None);
        var mine = await WeekHandler(context).HandleAsync(new GetMyLabourWeek(LastMonday), Email, CancellationToken.None);
        Assert.True(mine.Submission is { IsSentBack: true });
        Assert.All(mine.Days.Where(day => day.IsLogged), day => Assert.True(day.CanBeAmended));
        Assert.True(mine.CanBeSubmitted);
        var again = await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);
        Assert.True(again.Submission is { IsWithTheOffice: true });
        Assert.Single(await context.WorkerWeekSubmissions.ToListAsync());
    }

    [Fact]
    public async Task TheReviewList_showsWaitingWeeksFirst_withTheirDaysAndHours()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1), LastMonday.AddDays(2), LastMonday.AddDays(3), LastMonday.AddDays(4));
        await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);

        var list = await new ListWorkerWeekSubmissionsHandler(context, Reader(context)).HandleAsync(new ListWorkerWeekSubmissions(), CancellationToken.None);

        var week = Assert.Single(list);
        Assert.True(week.IsAwaitingReview);
        Assert.Equal("Jack Eastly", week.WorkerName);
        Assert.Equal(40m, week.TotalHours);
        Assert.Equal(5, week.Days.Count(day => day.IsLogged));
    }

    [Fact]
    public async Task ADayTheApprovalDoorRefuses_refusesTheWholeSignOff_byName()
    {
        await using var context = await SeededAsync();
        context.CostCodeBudgets.RemoveRange(await context.CostCodeBudgets.ToListAsync());
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1), LastMonday.AddDays(2), LastMonday.AddDays(3), LastMonday.AddDays(4));
        await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);
        var submission = await context.WorkerWeekSubmissions.SingleAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SignOffHandler(context).HandleAsync(new SignOffWorkerWeek(submission.WorkerWeekSubmissionId), Director, CancellationToken.None));

        Assert.Contains("No budget", refusal.Message);
        Assert.Contains("Abbot Road", refusal.Message);
        Assert.Equal((int)WorkerWeekSubmissionStatus.Submitted, (await context.WorkerWeekSubmissions.SingleAsync()).Status);
        Assert.Empty(await context.LabourWeekSignOffs.ToListAsync());
    }

    [Fact]
    public async Task AWeekAlreadyAnswered_cannotBeAnsweredAgain()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1), LastMonday.AddDays(2), LastMonday.AddDays(3), LastMonday.AddDays(4));
        await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);
        var submission = await context.WorkerWeekSubmissions.SingleAsync();
        await SignOffHandler(context).HandleAsync(new SignOffWorkerWeek(submission.WorkerWeekSubmissionId), Director, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SendBackHandler(context).HandleAsync(new SendBackWorkerWeek(submission.WorkerWeekSubmissionId, "Too late."), Director, CancellationToken.None));
    }
}
