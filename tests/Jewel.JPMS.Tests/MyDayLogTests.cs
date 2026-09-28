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

// Jeremy's model of the worker's day (2026-09-28): sign in on arrival as the health-and-safety
// record, then log the day once at sign-out — hours against one cost code, the words of what was
// done (required: they are the day's note on the progress feed) and photographs onto that note —
// or record a day off in words so a day is never simply missing.
public sealed class MyDayLogTests
{
    private const string Email = "jack@example.com";
    private const string UnlinkedEmail = "jack.easty@example.com";
    private const string Project = "p-abbot-road";
    private const string Code = "LAB";
    private const string NeverOpened = "Server=unused;Database=unused";
    private const string SortedInTheDatabase = "ORDER BY";

    [Fact]
    public async Task SigningOut_writesTheHours_theNote_andClosesTheAttendance_inOneSave()
    {
        await using var context = await SeededAsync();
        await SignInHandler(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);
        var entries = new[] { new SiteSignOutEntry(Code, WorkingDayChunks.FullDay) };

        var logged = await SignOutHandler(context).HandleAsync(
            new MySiteSignOut(Project, entries, "Second fix to the plot 3 bathrooms."), Email, CancellationToken.None);

        var timesheet = await context.Timesheets.SingleAsync();
        Assert.Equal(WorkingDayChunks.FullDay, timesheet.Hours);
        Assert.Equal((int)TimesheetStatus.Submitted, timesheet.Status);
        var note = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId);
        Assert.Equal("Daily log — Jack Eastly", note.Title);
        Assert.Equal(Email, note.CreatedByEmail);
        Assert.Equal(SiteClock.Today(), note.WorkDate);
        var attendance = await context.SiteAttendances.SingleAsync(row => row.SiteAttendanceId == logged.SiteAttendanceId);
        Assert.NotNull(attendance.SignedOutAt);
    }

    [Fact]
    public async Task SigningOut_needsTheWords_andASignOutTimeAfterTheSignIn()
    {
        await using var context = await SeededAsync();
        await SignInHandler(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);
        var entries = new[] { new SiteSignOutEntry(Code, WorkingDayChunks.HalfDay) };
        var handler = SignOutHandler(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MySiteSignOut(Project, entries, "   "), Email, CancellationToken.None));
        var beforeArrival = DateTimeOffset.UtcNow.AddDays(-1);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MySiteSignOut(Project, entries, "Half a day.", beforeArrival), Email, CancellationToken.None));
        Assert.Empty(await context.Timesheets.ToListAsync());
    }

    [Fact]
    public async Task ADayOff_isANoteAlone_andNeverSitsBesideASignIn()
    {
        await using var context = await SeededAsync();
        var handler = new MySiteDayOffHandler(context);

        var logged = await handler.HandleAsync(new MySiteDayOff(Project, "Rained off."), Email, CancellationToken.None);

        var note = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId);
        Assert.Equal("Off — Jack Eastly", note.Title);
        Assert.Equal("", logged.SiteAttendanceId);
        Assert.Empty(await context.SiteAttendances.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MySiteDayOff(Project, "Again."), Email, CancellationToken.None));
    }

    [Fact]
    public async Task ADayOff_isRefusedOnceTheWorkerHasSignedIn()
    {
        await using var context = await SeededAsync();
        await SignInHandler(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MySiteDayOffHandler(context).HandleAsync(new MySiteDayOff(Project, "Went home."), Email, CancellationToken.None));
    }

    [Fact]
    public async Task SigningIn_takesAnEarlierTimeToday_andRefusesOneInTheFuture()
    {
        await using var context = await SeededAsync();
        var handler = SignInHandler(context);
        var now = DateTimeOffset.UtcNow;
        var earlierToday = new[] { now.AddMinutes(-10), SiteClock.Today().AddMinutes(1) }.Max();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new MySiteSignIn(Project, now.AddHours(2)), Email, CancellationToken.None));
        await handler.HandleAsync(new MySiteSignIn(Project, earlierToday), Email, CancellationToken.None);

        var attendance = await context.SiteAttendances.SingleAsync();
        Assert.Equal(earlierToday, attendance.SignedInAt);
    }

    [Fact]
    public async Task TheDay_readsBackTheTimes_theNote_andTheCostCodes()
    {
        await using var context = await SeededAsync();
        await SignInHandler(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);
        await SignOutHandler(context).HandleAsync(
            new MySiteSignOut(Project, new[] { new SiteSignOutEntry(Code, WorkingDayChunks.HalfDay) }, "Half a day."), Email, CancellationToken.None);

        var day = await DayHandler(context).HandleAsync(Email, CancellationToken.None);

        Assert.True(day.IsLinked);
        var card = Assert.Single(day.Projects);
        Assert.True(card.IsLoggedToday);
        Assert.NotNull(card.SignedInAt);
        Assert.NotNull(card.SignedOutAt);
        Assert.Equal("Half a day.", card.TodaysNote?.Description);
        Assert.Equal(0, card.TodaysNote?.PhotoCount);
        Assert.Equal(Code, Assert.Single(card.CostCodes).Code);
        Assert.Equal(WorkingDayChunks.HalfDay, Assert.Single(day.Recent).Hours);
    }

    [Fact]
    public async Task ALoginWithNoWorkerRecord_isAnsweredWithAnUnlinkedDay_notAnError()
    {
        await using var context = await SeededAsync();

        var day = await DayHandler(context).HandleAsync(UnlinkedEmail, CancellationToken.None);

        Assert.False(day.IsLinked);
        Assert.Empty(day.Projects);
    }

    [Fact]
    public void TheDaysSortedReads_translateForSqlServer_notOnlyInMemory()
    {
        using var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseSqlServer(NeverOpened).Options);
        var worker = new WorkerEntity { WorkerId = "w-jack" };

        var assigned = DayProjects(context).AssignedTo(worker).ToQueryString();
        var timesheets = DayHandler(context).OwnTimesheets(worker, sheet => sheet.Hours > WorkingDayChunks.Off).ToQueryString();

        Assert.Contains(SortedInTheDatabase, assigned);
        Assert.Contains(SortedInTheDatabase, timesheets);
    }

    [Fact]
    public void TheDayInChunks_isOff_half_andFull()
    {
        Assert.Equal(0m, WorkingDayChunks.Off);
        Assert.Equal(4m, WorkingDayChunks.HalfDay);
        Assert.Equal(8m, WorkingDayChunks.FullDay);
    }

    private static MySiteSignInHandler SignInHandler(JpmsContext context) => new(context);

    private static MySiteSignOutHandler SignOutHandler(JpmsContext context) => new(context, new MyDayCostCodes(context));

    private static MyDayProjects DayProjects(JpmsContext context) =>
        new(context, new MyDayCostCodes(context), new MyDayNotesToday(context));

    private static GetMyLabourDayHandler DayHandler(JpmsContext context) => new(context, DayProjects(context));

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"my-day-{Guid.NewGuid():N}").Options);
        context.Workers.Add(new WorkerEntity { WorkerId = "w-jack", Name = "Jack Eastly", ContactEmail = Email, HourlyRate = 25m });
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Name = "Abbot Road" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-1", ProjectId = Project, WorkerId = "w-jack" });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "cc-lab", Code = Code, Name = "Labour" });
        await context.SaveChangesAsync();
        return context;
    }
}
