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

// Jeremy's second evening on Jack's phone (30 Sep 2026): the day can be logged against another of
// the worker's sites at sign-out, the trades offered are the ones the site has, and the office can
// read what was submitted with the day from the Labour tab.
public sealed class MyDaySiteAndTradesTests
{
    private const string Email = "jack@example.com";
    private const string Project = "p-abbot-road";
    private const string OtherProject = "p-ravenswood";
    private const string UnassignedProject = "p-elsewhere";
    private const string Plumbing = "MEC-PLM";
    private const string Demolition = "DEM";
    private const string Words = "Second fix to the plot 3 bathrooms.";
    private static readonly SiteSignOutEntry[] FullDay = { new(Plumbing, WorkingDayChunks.FullDay) };

    [Fact]
    public async Task SigningOut_ontoAnotherOfTheWorkersSites_writesTheWholeDayThere()
    {
        await using var context = await SeededAsync();
        await SignIn(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);

        var logged = await SignOut(context).HandleAsync(
            new MySiteSignOut(Project, FullDay, Words, SiteProjectId: OtherProject), Email, CancellationToken.None);

        var timesheet = await context.Timesheets.SingleAsync();
        Assert.Equal(OtherProject, timesheet.ProjectId);
        var note = await context.ProgressUpdates.SingleAsync(row => row.ProgressUpdateId == logged.ProgressUpdateId);
        Assert.Equal(OtherProject, note.ProjectId);
        var attendance = await context.SiteAttendances.SingleAsync(row => row.SiteAttendanceId == logged.SiteAttendanceId);
        Assert.Equal(OtherProject, attendance.ProjectId);
        Assert.NotNull(attendance.SignedOutAt);
    }

    [Fact]
    public async Task SigningOut_ontoASiteTheWorkerIsNotOn_isRefused()
    {
        await using var context = await SeededAsync();
        await SignIn(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SignOut(context).HandleAsync(
            new MySiteSignOut(Project, FullDay, Words, SiteProjectId: UnassignedProject), Email, CancellationToken.None));

        Assert.Empty(await context.Timesheets.ToListAsync());
        Assert.Null((await context.SiteAttendances.SingleAsync()).SignedOutAt);
    }

    [Fact]
    public async Task TheTradesOffered_areTheOnesTheSiteHas_andEveryTradeWhileItHasNone()
    {
        await using var context = await SeededAsync();
        context.BoqLineItems.Add(new BoqLineItemEntity { BoqLineItemId = "boq-1", ProjectId = Project, CostCode = Plumbing });
        context.WorkOrders.Add(new WorkOrderEntity { WorkOrderId = "wo-1", ProjectId = OtherProject });
        context.WorkOrderLines.Add(new WorkOrderLineEntity { WorkOrderLineId = "wol-1", WorkOrderId = "wo-1", CostCode = Demolition });
        await context.SaveChangesAsync();

        var codes = await new MyDayCostCodes(context).ByProjectAsync(new[] { Project, OtherProject, UnassignedProject }, CancellationToken.None);

        Assert.Equal(new[] { Plumbing }, codes[Project].Select(code => code.Code));
        Assert.Equal(new[] { Demolition }, codes[OtherProject].Select(code => code.Code));
        Assert.Equal(new[] { Demolition, Plumbing }, codes[UnassignedProject].Select(code => code.Code).OrderBy(code => code));
    }

    [Fact]
    public async Task SigningOut_againstATradeTheSiteDoesNotHave_isRefused()
    {
        await using var context = await SeededAsync();
        context.CostCodeBudgets.Add(new CostCodeBudgetEntity { CostCodeBudgetId = "b-1", ProjectId = Project, CostCode = Plumbing });
        await context.SaveChangesAsync();
        await SignIn(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);
        var demolition = new[] { new SiteSignOutEntry(Demolition, WorkingDayChunks.FullDay) };

        await Assert.ThrowsAsync<InvalidOperationException>(() => SignOut(context).HandleAsync(
            new MySiteSignOut(Project, demolition, Words), Email, CancellationToken.None));
    }

    [Fact]
    public async Task TheOffice_readsWhatWasSubmittedWithTheDay()
    {
        await using var context = await SeededAsync();
        await SignIn(context).HandleAsync(new MySiteSignIn(Project), Email, CancellationToken.None);
        var logged = await SignOut(context).HandleAsync(new MySiteSignOut(Project, FullDay, Words), Email, CancellationToken.None);
        context.ProgressPhotos.Add(new ProgressPhotoEntity { ProgressPhotoId = "ph-1", ProgressUpdateId = logged.ProgressUpdateId, ProjectId = Project, FileName = "bathroom.jpg" });
        await context.SaveChangesAsync();
        var timesheet = await context.Timesheets.SingleAsync();

        var day = await new GetSubmittedDayForTimesheetHandler(context).HandleAsync(new GetSubmittedDayForTimesheet(timesheet.TimesheetId), CancellationToken.None);

        Assert.NotNull(day);
        Assert.True(day!.HasNote);
        Assert.Equal(Words, day.Words);
        Assert.Equal("Jack Eastly", day.WorkerName);
        Assert.Equal("bathroom.jpg", Assert.Single(day.Photos).FileName);
        Assert.NotNull(day.SignedInAt);
        Assert.NotNull(day.SignedOutAt);
        Assert.Null(await new GetSubmittedDayForTimesheetHandler(context).HandleAsync(new GetSubmittedDayForTimesheet("nowhere"), CancellationToken.None));
    }

    private static MySiteSignInHandler SignIn(JpmsContext context) => new(context);

    private static MySiteSignOutHandler SignOut(JpmsContext context) => new(context, new MyDayCostCodes(context), new MyDayRaisedRecords(context));

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"my-day-sites-{Guid.NewGuid():N}").Options);
        context.Workers.Add(new WorkerEntity { WorkerId = "w-jack", Name = "Jack Eastly", ContactEmail = Email, HourlyRate = 25m });
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Name = "Abbot Road" });
        context.Projects.Add(new ProjectEntity { ProjectId = OtherProject, Name = "Ravenswood Ave" });
        context.Projects.Add(new ProjectEntity { ProjectId = UnassignedProject, Name = "Elsewhere" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-1", ProjectId = Project, WorkerId = "w-jack" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-2", ProjectId = OtherProject, WorkerId = "w-jack" });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "cc-plm", Code = Plumbing, Name = "Plumber", SortOrder = 2 });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "cc-dem", Code = Demolition, Name = "Demolition", SortOrder = 1 });
        await context.SaveChangesAsync();
        return context;
    }
}
