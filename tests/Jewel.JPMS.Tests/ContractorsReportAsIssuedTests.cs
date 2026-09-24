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

// The Contractor's Report composed as issued Report 30 reads (24 Sep 2026, from By France Report
// 31): Section 7's entered contact when the project has no Building Control case, Section 8's
// scope in the report's own words, Section 9 without the photographs left out, and a new report
// opening on the current valuation and the Friday after its week.
public sealed class ContractorsReportAsIssuedTests
{
    private const string Project = "by-france";
    private const string Bromley = "Bromley Building Control — buildingcontrol@bromley.gov.uk";
    private static readonly ReportingWeek Week = ReportingWeek.EndingOn(new DateOnly(2026, 9, 24));

    [Fact]
    public async Task TheReport_printsTheEnteredContact_theWeeksScope_andOnlyThePhotographsLeftIn()
    {
        await using var context = await SeededContext();

        var view = (await new ContractorsReportComposer(context).ViewAsync("report-31", CancellationToken.None))!;

        Assert.False(view.Document.BuildingControl.HasCase());
        Assert.Equal(Bromley, view.Document.BuildingControl.EnteredContact);
        Assert.Equal("Wall tiling to the first floor (Friday)", Assert.Single(view.Document.Subcontractors).Scope);
        Assert.Equal(new[] { "photo-kept" }, view.Document.Progress.SelectMany(day => day.Photos).Select(photo => photo.ProgressPhotoId));
        Assert.Equal(2, view.PhotosOnSelectedUpdates.Count);
        Assert.False(view.PhotosOnSelectedUpdates.Single(photo => photo.ProgressPhotoId == "photo-left-out").IsIncluded);
    }

    [Fact]
    public async Task ANewReport_opensOnTheCurrentValuation_theFridayAfter_andCarriesTheContact()
    {
        await using var context = await SeededContext();
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = "v20", ProjectId = Project, ClaimNumber = 3, Name = "Valuation 20 - August 2026", Status = (int)ValuationClaimStatus.Confirmed, ClaimDate = new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero) });
        context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = "v21", ProjectId = Project, ClaimNumber = 4, Name = "Valuation 21 - September 2026", Status = (int)ValuationClaimStatus.Preapproved, ClaimDate = new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero) });
        await context.SaveChangesAsync();

        var next = await new CreateContractorsReportHandler(context).HandleAsync(
            new CreateContractorsReport(Project, Week.End.AddDays(7)), CancellationToken.None);

        Assert.Equal("21", next.ValuationNumber);
        Assert.Equal(new DateOnly(2026, 10, 2), next.DateOfIssue);
        Assert.Equal(Bromley, next.BuildingControlContact);
    }

    [Theory]
    [InlineData("Valuation 21 - September 2026", "21")]
    [InlineData("valuation no. 7", "7")]
    [InlineData("September 2026", null)]
    public void TheValuationNumber_isReadFromItsName(string name, string? expected) =>
        Assert.Equal(expected, ContractorsReportValuations.NumberIn(name));

    private static async Task<JpmsContext> SeededContext()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"contractors-report-as-issued-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sussex", CompanyName = "Sussex Tiling and Mastic Ltd" });
        context.WorkOrders.Add(new WorkOrderEntity { WorkOrderId = "wo-49", ProjectId = Project, Number = 49, SubcontractorId = "sussex", Title = "Tiling package", Status = (int)WorkOrderStatus.Released });
        context.ProgressUpdates.Add(new ProgressUpdateEntity { ProgressUpdateId = "friday", ProjectId = Project, Title = "Friday", WorkDate = new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.Zero) });
        context.ProgressPhotos.Add(new ProgressPhotoEntity { ProgressPhotoId = "photo-kept", ProgressUpdateId = "friday", ProjectId = Project, FileName = "tiling.jpg", SortOrder = 1 });
        context.ProgressPhotos.Add(new ProgressPhotoEntity { ProgressPhotoId = "photo-left-out", ProgressUpdateId = "friday", ProjectId = Project, FileName = "pallet.jpg", SortOrder = 2 });
        context.ContractorsReports.Add(new ContractorsReportEntity
        {
            ContractorsReportId = "report-31", ProjectId = Project, Number = 31,
            PeriodStart = Week.Start, PeriodEnd = Week.End, DateOfIssue = Week.End.AddDays(1),
            BuildingControlContact = Bromley,
            AttendanceJson = ContractorsReportJson.Write(new[] { new ContractorsReportAttendance("wo-49", 4, false, "Wall tiling to the first floor (Friday)") }),
            SelectedUpdateIdsJson = ContractorsReportJson.Write(new[] { "friday" }),
            ExcludedPhotoIdsJson = ContractorsReportJson.Write(new[] { "photo-left-out" })
        });
        await context.SaveChangesAsync();
        return context;
    }
}
