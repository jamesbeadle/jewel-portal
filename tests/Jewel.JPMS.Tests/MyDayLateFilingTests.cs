using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The operative corrects their own week from My day (Jeremy on Jack's phone, 30 Sep 2026): a day
// logged against the wrong site moves, a day that reads "Nothing" is filled in or recorded off
// after the date, and anything filed after the working day is marked late for the office.
public sealed class MyDayLateFilingTests
{
    private const string Email = "jack@example.com";
    private const string Project = "p-abbot-road";
    private const string OtherProject = "p-ravenswood";
    private const string UnassignedProject = "p-elsewhere";
    private const string Code = "LAB";
    private const string Words = "Second fix to the plot 3 bathrooms.";
    private static readonly SiteSignOutEntry[] FullDay = { new(Code, WorkingDayChunks.FullDay) };

    [Fact]
    public async Task AMissedDay_isATimesheetMarkedLate_andANote_withNoAttendance()
    {
        await using var context = await SeededAsync();
        var yesterday = SiteClock.Today().AddDays(-1);

        var logged = await MissedDay(context).HandleAsync(
            new MyLogMissedSiteDay(Project, yesterday, WorkingDayChunks.FullDay, Code, Words), Email, CancellationToken.None);

        var timesheet = await context.Timesheets.SingleAsync();
        Assert.Equal(yesterday, timesheet.WorkedOn);
        Assert.Equal((int)TimesheetStatus.Submitted, timesheet.Status);
        Assert.True(timesheet.IsFiledLate);
        Assert.Equal("", timesheet.SiteAttendanceId);
        Assert.Empty(await context.SiteAttendances.ToListAsync());
        var note = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId);
        Assert.Equal(yesterday, note.WorkDate);
        Assert.Equal("Daily log — Jack Eastly", note.Title);
    }

    [Fact]
    public async Task AMissedDay_isRefusedForToday_forADayTooLongAgo_andForADayAlreadyThere()
    {
        await using var context = await SeededAsync();
        var handler = MissedDay(context);
        var today = SiteClock.Today();
        var tooLongAgo = today - LabourRules.LateFilingWindow - TimeSpan.FromDays(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MyLogMissedSiteDay(Project, today, WorkingDayChunks.FullDay, Code, Words), Email, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MyLogMissedSiteDay(Project, tooLongAgo, WorkingDayChunks.FullDay, Code, Words), Email, CancellationToken.None));
        await handler.HandleAsync(new MyLogMissedSiteDay(Project, today.AddDays(-1), WorkingDayChunks.FullDay, Code, Words), Email, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MyLogMissedSiteDay(Project, today.AddDays(-1), WorkingDayChunks.HalfDay, Code, Words), Email, CancellationToken.None));

        Assert.Single(await context.Timesheets.ToListAsync());
    }

    [Fact]
    public async Task ADayOff_canBeRecordedForADayAlreadyGone_andTheWeekReadsIt()
    {
        await using var context = await SeededAsync();
        var yesterday = SiteClock.Today().AddDays(-1);

        var logged = await new MySiteDayOffHandler(context).HandleAsync(
            new MySiteDayOff(Project, "Rained off.", yesterday), Email, CancellationToken.None);

        var note = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId);
        Assert.Equal(yesterday, note.WorkDate);
        Assert.Equal("Off — Jack Eastly", note.Title);
    }

    [Fact]
    public async Task AmendingTheDay_movesItToAnotherOfTheWorkersSites_andNeverToOneTheyAreNotOn()
    {
        await using var context = await SeededAsync();
        var logged = await LogDayAsync(context);
        var timesheet = await context.Timesheets.SingleAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Amend(context).HandleAsync(
            new MyAmendSiteDay(timesheet.TimesheetId, WorkingDayChunks.FullDay, Code, Words, ProjectId: UnassignedProject), Email, CancellationToken.None));
        await Amend(context).HandleAsync(
            new MyAmendSiteDay(timesheet.TimesheetId, WorkingDayChunks.FullDay, Code, Words, ProjectId: OtherProject), Email, CancellationToken.None);

        var moved = await context.Timesheets.SingleAsync();
        Assert.Equal(OtherProject, moved.ProjectId);
        Assert.False(moved.IsFiledLate);
        var note = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId);
        Assert.Equal(OtherProject, note.ProjectId);
        var attendance = await context.SiteAttendances.SingleAsync(row => row.SiteAttendanceId == logged.SiteAttendanceId);
        Assert.Equal(OtherProject, attendance.ProjectId);
    }

    [Fact]
    public async Task ADayAmendedAfterItsDate_isMarkedLate_andTheWeekCarriesItsNote()
    {
        await using var context = await SeededAsync();
        var logged = await LogDayAsync(context);
        var yesterday = SiteClock.Today().AddDays(-1);
        await MoveTheDayBackAsync(context, logged, yesterday);

        await Amend(context).HandleAsync(
            new MyAmendSiteDay((await context.Timesheets.SingleAsync()).TimesheetId, WorkingDayChunks.HalfDay, Code, "Half a day after all."), Email, CancellationToken.None);

        Assert.True((await context.Timesheets.SingleAsync()).IsFiledLate);
        var day = await new GetMyLabourDayHandler(context, DayProjects(context), new MyDayWeek(context)).HandleAsync(Email, CancellationToken.None);
        var row = day.Week.Single(candidate => candidate.ProjectId == Project && candidate.Date == yesterday);
        Assert.True(row.IsFiledLate);
        Assert.Equal(logged.ProgressUpdateId, row.ProgressUpdateId);
        Assert.True(row.HasNote);
    }

    [Fact]
    public void TheLateFilingWindow_isAFortnightOfDaysAlreadyGone()
    {
        var today = SiteClock.Today();

        Assert.False(LabourRules.IsOpenForLateFiling(today, today));
        Assert.True(LabourRules.IsOpenForLateFiling(today.AddDays(-1), today));
        Assert.True(LabourRules.IsOpenForLateFiling(today - LabourRules.LateFilingWindow, today));
        Assert.False(LabourRules.IsOpenForLateFiling(today - LabourRules.LateFilingWindow - TimeSpan.FromDays(1), today));
    }

    private static async Task MoveTheDayBackAsync(JpmsContext context, MySiteDayLogged logged, DateTimeOffset workDate)
    {
        var timesheet = await context.Timesheets.SingleAsync();
        timesheet.WorkedOn = workDate;
        var note = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId);
        note.WorkDate = workDate;
        var attendance = await context.SiteAttendances.SingleAsync(row => row.SiteAttendanceId == logged.SiteAttendanceId);
        attendance.WorkDate = workDate;
        attendance.SignedInAt = workDate.AddHours(8);
        attendance.SignedOutAt = workDate.AddHours(16);
        await context.SaveChangesAsync();
    }

    private static async Task<MySiteDayLogged> LogDayAsync(JpmsContext context)
    {
        await new MySiteSignInHandler(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);
        var handler = new MySiteSignOutHandler(context, new MyDayCostCodes(context), new MyDayRaisedRecords(context));
        return await handler.HandleAsync(new MySiteSignOut(Project, FullDay, Words), Email, CancellationToken.None);
    }

    private static MyLogMissedSiteDayHandler MissedDay(JpmsContext context) => new(context, new MyDayCostCodes(context));

    private static MyAmendSiteDayHandler Amend(JpmsContext context) => new(context, new MyDayCostCodes(context));

    private static MyDayProjects DayProjects(JpmsContext context) =>
        new(context, new MyDayCostCodes(context), new MyDayNotesToday(context));

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"my-day-late-{Guid.NewGuid():N}").Options);
        context.Workers.Add(new WorkerEntity { WorkerId = "w-jack", Name = "Jack Eastly", ContactEmail = Email, HourlyRate = 25m });
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Name = "Abbot Road" });
        context.Projects.Add(new ProjectEntity { ProjectId = OtherProject, Name = "Ravenswood Ave" });
        context.Projects.Add(new ProjectEntity { ProjectId = UnassignedProject, Name = "Elsewhere" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-1", ProjectId = Project, WorkerId = "w-jack" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-2", ProjectId = OtherProject, WorkerId = "w-jack" });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "cc-lab", Code = Code, Name = "Labour" });
        await context.SaveChangesAsync();
        return context;
    }
}

// A day signed in on the wrong site is put right at sign-out (Jeremy on Jack's phone, 30 Sep 2026,
// 22:53: "So, I still can't change site?"): the open sign-in moves to the site the worker names.
public sealed class MyDaySignOutSiteTests
{
    private const string Email = "jack@example.com";
    private const string Project = "p-abbot-road";
    private const string OtherProject = "p-ravenswood";
    private const string UnassignedProject = "p-elsewhere";
    private const string Code = "LAB";
    private static readonly SiteSignOutEntry[] FullDay = { new(Code, WorkingDayChunks.FullDay) };

    [Fact]
    public async Task SigningOutOnAnotherSite_movesTheOpenSignInThere_andRefusesASiteTheWorkerIsNotOn()
    {
        await using var context = await SeededAsync();
        await new MySiteSignInHandler(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);
        var handler = new MySiteSignOutHandler(context, new MyDayCostCodes(context), new MyDayRaisedRecords(context));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MySiteSignOut(UnassignedProject, FullDay, "Wrong site."), Email, CancellationToken.None));
        var logged = await handler.HandleAsync(new MySiteSignOut(OtherProject, FullDay, "Second fix at Ravenswood."), Email, CancellationToken.None);

        var attendance = await context.SiteAttendances.SingleAsync();
        Assert.Equal(OtherProject, attendance.ProjectId);
        Assert.NotNull(attendance.SignedOutAt);
        Assert.Equal(OtherProject, (await context.Timesheets.SingleAsync()).ProjectId);
        Assert.Equal(OtherProject, (await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId)).ProjectId);
    }

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"my-day-site-{Guid.NewGuid():N}").Options);
        context.Workers.Add(new WorkerEntity { WorkerId = "w-jack", Name = "Jack Eastly", ContactEmail = Email, HourlyRate = 25m });
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Name = "Abbot Road" });
        context.Projects.Add(new ProjectEntity { ProjectId = OtherProject, Name = "Ravenswood Ave" });
        context.Projects.Add(new ProjectEntity { ProjectId = UnassignedProject, Name = "Elsewhere" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-1", ProjectId = Project, WorkerId = "w-jack" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-2", ProjectId = OtherProject, WorkerId = "w-jack" });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "cc-lab", Code = Code, Name = "Labour" });
        await context.SaveChangesAsync();
        return context;
    }
}
