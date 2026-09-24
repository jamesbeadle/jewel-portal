using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Section 8 prints the firms that attended, and only them (By France Report 31, 24 Sep 2026: all
// 54 live orders were composed in, and an order that was not on site — "extra labour to rectify
// pipework" — tripped the wording gate and refused the Word and PDF). Every live order is still
// offered on the page to enter attendance against.
public sealed class ContractorsReportSection8Tests
{
    private const string Project = "by-france";
    private static readonly ReportingWeek Week = ReportingWeek.EndingOn(new DateOnly(2026, 9, 24));

    [Fact]
    public async Task OnlyTheOrdersThatAttended_printInSection8_andAnAbsentOrdersWording_isNeverChecked()
    {
        await using var context = await SeededContext();

        var view = await new ContractorsReportComposer(context).ViewAsync("report-31", CancellationToken.None);

        Assert.Equal(new[] { "Sussex Tiling and Mastic Ltd" }, view!.Document.Subcontractors.Select(order => order.Supplier));
        Assert.True(view.Document.CanBeBuilt);
        Assert.Equal(3, view.WorkOrdersOnSite.Count);
    }

    private static async Task<JpmsContext> SeededContext()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"contractors-report-section-8-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sussex", CompanyName = "Sussex Tiling and Mastic Ltd" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "jp-air", CompanyName = "JP Air Conditioning" });
        context.WorkOrders.AddRange(
            Order("wo-49", 49, "sussex", "Tiling to the bathrooms"),
            Order("wo-54", 54, "jp-air", "Extra labour to rectify pipework"),
            Order("wo-26", 26, "jp-air", "Second fix electrics"));
        context.ContractorsReports.Add(new ContractorsReportEntity
        {
            ContractorsReportId = "report-31", ProjectId = Project, Number = 31,
            PeriodStart = Week.Start, PeriodEnd = Week.End, DateOfIssue = Week.End.AddDays(1),
            AttendanceJson = ContractorsReportJson.Write(new[]
            {
                new ContractorsReportAttendance("wo-49", 4, false),
                new ContractorsReportAttendance("wo-26", 0, false)
            })
        });
        await context.SaveChangesAsync();
        return context;
    }

    private static WorkOrderEntity Order(string id, int number, string supplier, string title) => new()
    {
        WorkOrderId = id, ProjectId = Project, Number = number, SubcontractorId = supplier,
        Title = title, Status = (int)WorkOrderStatus.Released
    };
}
