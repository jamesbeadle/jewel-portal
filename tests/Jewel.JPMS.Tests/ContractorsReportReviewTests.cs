using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Jeremy's review of By France Report 31 (24 Sep 2026), read against issued Report 30: a day's
// note prints as bullets, Section 3 leaves out what is still in-house and says when a response
// was due, Section 8 names the days a firm was there, and Section 1 ends with what was received.
public sealed class ContractorsReportReviewTests
{
    private const string Project = "by-france";
    private static readonly ReportingWeek Week = ReportingWeek.EndingOn(new DateOnly(2026, 9, 24));
    private static readonly DateTimeOffset Tuesday = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ADaysNote_printsOneBulletPerLine_withoutTheMarksAlreadyTyped()
    {
        var bullets = ContractorsReportPrintedText.Bullets("- Tiling to En-Suite 1\n\n• Render first coat\r\nSkirtings fixed");
        Assert.Equal(new[] { "Tiling to En-Suite 1", "Render first coat", "Skirtings fixed" }, bullets);
    }

    [Fact]
    public void AnRfi_isAwaitingResponse_andSaysWhenItWasDue_onceThatDateHasPassed()
    {
        var overdue = new ContractorsReportDecision("r1", "RFI-032", "Landscape Plan", "Open", new DateOnly(2026, 9, 12));
        var notYetDue = overdue with { ResponseDue = new DateOnly(2026, 10, 1) };
        var issued = new DateOnly(2026, 9, 25);

        Assert.Equal("Landscape Plan (RFI-032)", ContractorsReportPrintedText.DecisionRow(overdue));
        Assert.Equal("Awaiting response, response was due 12 September 2026", ContractorsReportPrintedText.DecisionStatus(overdue, issued));
        Assert.Equal("Awaiting response", ContractorsReportPrintedText.DecisionStatus(notYetDue, issued));
    }

    [Fact]
    public void AFirmsDaysOnSite_readAsTheWeekdays()
    {
        var firm = new ContractorsReportSubcontractor("wo", "WO-0049", "Sussex Tiling", "Tiling", "Tiling", 0m, null, 3, false,
            new[] { new DateOnly(2026, 9, 23), new DateOnly(2026, 9, 18), new DateOnly(2026, 9, 21) });
        Assert.Equal("Friday, Monday, Wednesday", ContractorsReportPrintedText.DaysOnSite(firm));
    }

    [Fact]
    public async Task Section3_leavesOutRfisAtNeedsAction_andSection1_endsWithWhatWasReceived()
    {
        await using var context = await SeededContext();

        var document = (await new ContractorsReportComposer(context).ViewAsync("report-31", CancellationToken.None))!.Document;

        Assert.Equal(new[] { "RFI-049" }, document.Decisions.Select(decision => decision.Reference));
        Assert.Equal(new[] { "Kitchen Installation Questions (RFI-049) raised on 22 September 2026", "AI-07 — Revised render colour, received on 22 September 2026" }
            .OrderBy(line => line), document.InstructionsReceived.OrderBy(line => line));
    }

    private static async Task<JpmsContext> SeededContext()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"contractors-report-review-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France" });
        context.Requests.Add(new RequestEntity { RequestId = "rfi-49", ProjectId = Project, Kind = (int)RequestType.Rfi, Reference = "RFI-049", Title = "Kitchen Installation Questions", Status = (int)RequestStatus.Open, RaisedAt = Tuesday, IssuedAt = Tuesday });
        context.Requests.Add(new RequestEntity { RequestId = "rfi-50", ProjectId = Project, Kind = (int)RequestType.Rfi, Reference = "RFI-050", Title = "Still being checked", Status = (int)RequestStatus.NeedsAction, RaisedAt = Tuesday });
        context.ArchitectInstructions.Add(new ArchitectInstructionEntity { ArchitectInstructionId = "ai-7", ProjectId = Project, Reference = "AI-0007", InstructionRef = "AI-07", Title = "Revised render colour", ReceivedAt = Tuesday });
        context.ContractorsReports.Add(new ContractorsReportEntity
        {
            ContractorsReportId = "report-31", ProjectId = Project, Number = 31,
            PeriodStart = Week.Start, PeriodEnd = Week.End, DateOfIssue = Week.End.AddDays(1)
        });
        await context.SaveChangesAsync();
        return context;
    }
}
