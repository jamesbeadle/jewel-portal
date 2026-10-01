using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.MyDayWeeksSeed;

namespace Jewel.JPMS.Tests;

/// <summary>Jeremy's asks of 1 Oct 2026: an operative swipes back through earlier weeks, reads the
/// month as they invoice it, and sends a finished week to the office in one step.</summary>
public sealed class MyDayWeeksTests
{
    [Fact]
    public async Task AnEarlierWeek_readsEveryWorkingDay_loggedOrNothing_namedByAnyDateInIt()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1));

        var week = await WeekHandler(context).HandleAsync(new GetMyLabourWeek(LastMonday.AddDays(3)), Email, CancellationToken.None);

        Assert.Equal(LastMonday, week.WeekStart);
        var days = week.Days;
        Assert.Equal(LabourWeeks.WorkingDaysInWeek, days.Count);
        Assert.Equal(2, days.Count(day => day.IsLogged));
        Assert.True(days.Single(day => day.Date == LastMonday).CanBeAmended);
        Assert.Null(week.Submission);
    }

    [Fact]
    public async Task AWeekStillToCome_isRefused()
    {
        await using var context = await SeededAsync();
        var nextMonday = LabourWeeks.MondayOf(SiteClock.Today()).AddDays(LabourWeeks.DaysInWeek);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WeekHandler(context).HandleAsync(new GetMyLabourWeek(nextMonday), Email, CancellationToken.None));
    }

    [Fact]
    public async Task ADayTheOfficePlannedOff_readsAsPlannedOff()
    {
        await using var context = await SeededAsync();
        context.WorkerAbsences.Add(new WorkerAbsenceEntity { WorkerAbsenceId = "ab-1", WorkerId = WorkerId, Date = LastMonday, Kind = (int)AbsenceKind.Holiday });
        await context.SaveChangesAsync();

        var week = await WeekHandler(context).HandleAsync(new GetMyLabourWeek(LastMonday), Email, CancellationToken.None);

        Assert.Equal(AbsenceKind.Holiday, week.Days.Single(day => day.Date == LastMonday).PlannedAbsence);
    }

    [Fact]
    public async Task TheMonth_countsApprovedHoursAsDays_andWhatIsStillOutstanding()
    {
        await using var context = await SeededAsync();
        var first = new DateTimeOffset(new DateTime(2026, 6, 1), TimeSpan.Zero);
        await LogDaysAsync(context, first, first.AddDays(1), first.AddDays(2));
        var approved = await context.Timesheets.FirstAsync(row => row.WorkedOn == first);
        approved.Status = (int)TimesheetStatus.Approved;
        await RecordOffAsync(context, first.AddDays(3));
        await context.SaveChangesAsync();

        var month = await MonthHandler(context).HandleAsync(new GetMyLabourMonth(2026, 6), Email, CancellationToken.None);

        var totals = month.Totals;
        Assert.Equal(8m, totals.ApprovedHours);
        Assert.Equal(1m, totals.ApprovedDays);
        Assert.Equal(2, totals.WaitingDays);
        Assert.Equal(1, totals.OffDays);
        Assert.Equal(ForecastRules.WorkingDaysInMonth(2026, 6) - 4, totals.MissingDays);
        Assert.Equal(totals.WaitingDays + totals.MissingDays, totals.OutstandingDays);
    }

    [Fact]
    public async Task AWeek_isSubmittedOnceEveryWorkingDayIsAccountedFor_andLocks()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1), LastMonday.AddDays(2), LastMonday.AddDays(3));
        var incomplete = await WeekHandler(context).HandleAsync(new GetMyLabourWeek(LastMonday), Email, CancellationToken.None);
        Assert.False(incomplete.CanBeSubmitted);
        Assert.Contains("nothing logged", incomplete.SubmitRefusal);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None));
        await RecordOffAsync(context, LastMonday.AddDays(4));

        var submitted = await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);

        Assert.True(submitted.Submission is { IsWithTheOffice: true });
        Assert.True(submitted.IsLocked);
        Assert.All(submitted.Days, day => Assert.False(day.CanBeAmended));
        Assert.False(submitted.CanBeSubmitted);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WorkerWeekLock.EnsureOpenAsync(context, WorkerId, LastMonday.AddDays(2), CancellationToken.None));
    }

    [Fact]
    public async Task TheCurrentWeek_waitsForFriday()
    {
        await using var context = await SeededAsync();
        var thisMonday = LabourWeeks.MondayOf(SiteClock.Today());
        var isFridayGone = SiteClock.Today() >= thisMonday.AddDays(4);

        var week = await WeekHandler(context).HandleAsync(new GetMyLabourWeek(thisMonday), Email, CancellationToken.None);

        Assert.False(week.CanBeSubmitted);
        if (!isFridayGone) Assert.Contains("Friday", week.SubmitRefusal);
    }
}
