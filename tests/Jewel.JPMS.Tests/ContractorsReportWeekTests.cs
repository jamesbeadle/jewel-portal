using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The week composes from the workers' own logs (Jeremy, 21 Sep 2026): the composer lists every
// day of the period with who filed and who signed in and did not, a recorded day off reads whole,
// and a firm's days on site come off its workers' sign-ins rather than off memory.
public sealed class ContractorsReportWeekTests
{
    private const string Project = "abbot-road";
    private const string JackEmail = "jack@example.com";
    private const string PranasEmail = "pranas@example.com";
    private const string OfficeEmail = "jeremy@jewelbb.co.uk";
    private static readonly ReportingWeek Week = ReportingWeek.EndingOn(new DateOnly(2026, 10, 8));
    private static readonly DateOnly Monday = new(2026, 10, 5);
    private static readonly DateOnly Tuesday = new(2026, 10, 6);
    private static readonly DateOnly Wednesday = new(2026, 10, 7);

    [Fact]
    public async Task TheWeek_saysWhoFiled_whoWasOnSiteAndDidNot_andWhichDayHoldsNothing()
    {
        await using var context = await SeededAsync();

        var view = await new ContractorsReportComposer(context).ViewAsync("report-32", CancellationToken.None);

        var week = view!.Week;
        Assert.Equal(7, week.Count);
        var monday = week.Single(day => day.Date == Monday);
        Assert.Equal(new[] { "Jack Eastly" }, monday.Filed.Select(filing => filing.FiledBy));
        Assert.Equal(ContractorsReportFilingKind.DailyLog, monday.Filed[0].Kind);
        Assert.Equal(2, monday.PhotoCount);
        Assert.Equal(new[] { "Pranas Kairys" }, monday.OnSiteNotFiled);
        Assert.True(monday.NeedsAsking);
        var tuesday = week.Single(day => day.Date == Tuesday);
        Assert.Equal(ContractorsReportFilingKind.OffDay, Assert.Single(tuesday.Filed).Kind);
        Assert.True(tuesday.IsWhole);
        var wednesday = week.Single(day => day.Date == Wednesday);
        var officeNote = Assert.Single(wednesday.Filed);
        Assert.Equal(ContractorsReportFilingKind.Note, officeNote.Kind);
        Assert.Equal(OfficeEmail, officeNote.FiledBy);
        Assert.False(officeNote.IsSelected);
        var thursday = week.Single(day => day.Date == Week.End);
        Assert.True(thursday.HasNothingRecorded);
        Assert.True(thursday.NeedsAsking);
        Assert.False(week.Single(day => day.Date == Week.Start.AddDays(1)).NeedsAsking);
    }

    [Fact]
    public async Task AFirmsDaysOnSite_comeOffItsWorkersSignIns()
    {
        await using var context = await SeededAsync();

        var view = await new ContractorsReportComposer(context).ViewAsync("report-32", CancellationToken.None);

        var carpentry = view!.WorkOrdersOnSite.Single(order => order.WorkOrderId == "wo-carpentry");
        Assert.Equal(new[] { Monday, Tuesday }, carpentry.DaysSignedIn);
        Assert.Empty(view.WorkOrdersOnSite.Single(order => order.WorkOrderId == "wo-tiling").DaysSignedIn);
    }

    [Fact]
    public async Task ANewReport_opensWithTheSignInsTicked_onAFirmsOneLiveOrder()
    {
        await using var context = await SeededAsync();
        var handler = new CreateContractorsReportHandler(context);

        var report = await handler.HandleAsync(new CreateContractorsReport(Project, Week.End.AddDays(7)), CancellationToken.None);

        Assert.Empty(report.Attendance);
        var lastWeek = await handler.HandleAsync(new CreateContractorsReport("ravenswood", Week.End), CancellationToken.None);
        var ticked = Assert.Single(lastWeek.Attendance);
        Assert.Equal("wo-ravenswood-carpentry", ticked.WorkOrderId);
        Assert.Equal(new[] { Wednesday }, ticked.DaysOnSite);
        Assert.Equal(1, ticked.AttendanceDays);
    }

    [Fact]
    public void AFirmWithTwoLiveOrders_isLeftToThePerson()
    {
        var firm = "Eastly Carpentry";
        var live = new[]
        {
            Order("wo-1", firm, new[] { Monday }),
            Order("wo-2", firm, new[] { Monday }),
            Order("wo-3", "Sussex Tiling", new[] { Tuesday })
        };

        var attendance = ContractorsReportSubcontractorsReader.FromSignIns(live);

        Assert.Equal("wo-3", Assert.Single(attendance).WorkOrderId);
    }

    private static ContractorsReportSubcontractor Order(string id, string supplier, IReadOnlyList<DateOnly> signedIn) =>
        new(id, id.ToUpperInvariant(), supplier, "Scope", "Title", 0m, null, null, false, Array.Empty<DateOnly>(), signedIn);

    private static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"contractors-report-week-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-002", Name = "Abbot Road" });
        context.Projects.Add(new ProjectEntity { ProjectId = "ravenswood", Reference = "JBB-2026-003", Name = "Ravenswood Ave" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "eastly", CompanyName = "Eastly Carpentry" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sussex", CompanyName = "Sussex Tiling" });
        context.Workers.Add(new WorkerEntity { WorkerId = "w-jack", Name = "Jack Eastly", ContactEmail = JackEmail, SubcontractorId = "eastly" });
        context.Workers.Add(new WorkerEntity { WorkerId = "w-pranas", Name = "Pranas Kairys", ContactEmail = PranasEmail, SubcontractorId = "eastly" });
        context.WorkOrders.AddRange(
            WorkOrder("wo-carpentry", Project, 12, "eastly"),
            WorkOrder("wo-tiling", Project, 13, "sussex"),
            WorkOrder("wo-ravenswood-carpentry", "ravenswood", 3, "eastly"));
        context.SiteAttendances.AddRange(
            SignIn("a-1", Project, "w-jack", Monday),
            SignIn("a-2", Project, "w-pranas", Monday),
            SignIn("a-3", Project, "w-jack", Tuesday),
            SignIn("a-4", "ravenswood", "w-pranas", Wednesday));
        context.ProgressUpdates.AddRange(
            Note("u-jack-mon", Project, "Daily log — Jack Eastly", JackEmail, Monday),
            Note("u-jack-tue", Project, "Off — Jack Eastly", JackEmail, Tuesday),
            Note("u-office-wed", Project, "Site visit", OfficeEmail, Wednesday));
        context.ProgressPhotos.AddRange(Photo("ph-1", "u-jack-mon"), Photo("ph-2", "u-jack-mon"));
        context.ContractorsReports.Add(new ContractorsReportEntity
        {
            ContractorsReportId = "report-32", ProjectId = Project, Number = 32,
            PeriodStart = Week.Start, PeriodEnd = Week.End, DateOfIssue = Week.End.AddDays(1),
            SelectedUpdateIdsJson = ContractorsReportJson.Write(new[] { "u-jack-mon", "u-jack-tue" })
        });
        await context.SaveChangesAsync();
        return context;
    }

    private static WorkOrderEntity WorkOrder(string id, string projectId, int number, string supplier) => new()
    {
        WorkOrderId = id, ProjectId = projectId, Number = number, SubcontractorId = supplier, Title = "Works",
        Status = (int)WorkOrderStatus.Released
    };

    private static SiteAttendanceEntity SignIn(string id, string projectId, string workerId, DateOnly day) => new()
    {
        SiteAttendanceId = id, ProjectId = projectId, WorkerId = workerId,
        WorkDate = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
        SignedInAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(7, 30)), TimeSpan.Zero)
    };

    private static ProgressUpdateEntity Note(string id, string projectId, string title, string email, DateOnly day) => new()
    {
        ProgressUpdateId = id, ProjectId = projectId, Title = title, Description = "Words.", CreatedByEmail = email,
        WorkDate = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), CreatedAt = DateTimeOffset.UtcNow
    };

    private static ProgressPhotoEntity Photo(string id, string updateId) => new()
    {
        ProgressPhotoId = id, ProgressUpdateId = updateId, ProjectId = Project, FileName = $"{id}.jpg", ContentType = "image/jpeg"
    };
}
