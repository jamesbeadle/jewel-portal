using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Section 4 as issued Report 30 printed it (Nigel, 24 Sep 2026): the variations at Issued only,
// Variation / Position with the issue date and no value column, opened by the ones approved in
// the period. Report 31 had printed V19, V71 and V88 at Quoting with £0.00 beside them.
public sealed class ContractorsReportSection4Tests
{
    private const string Project = "by-france";
    private static readonly ReportingWeek Week = ReportingWeek.EndingOn(new DateOnly(2026, 9, 24));

    [Fact]
    public async Task OnlyIssuedVariationsPrint_withTheirIssueDate_andThoseApprovedInThePeriodOpenTheSection()
    {
        await using var context = await SeededContext();

        var document = (await new ContractorsReportComposer(context).ViewAsync("report-31", CancellationToken.None))!.Document;

        var row = Assert.Single(document.Variations);
        Assert.Equal("V83 — Extended Preliminaries, EOT-05 (10 weeks)", ContractorsReportVariationWording.Row(row));
        Assert.Equal("Issued 4 August 2026 — no response received", ContractorsReportVariationWording.Position(row));
        Assert.EndsWith("V84 was approved on 22 September 2026 and is no longer outstanding.",
            ContractorsReportVariationWording.Paragraph(document));
    }

    [Fact]
    public void SeveralApprovedOnOneDay_readAsOneSentence()
    {
        var approved = new[]
        {
            new ContractorsReportApprovedVariation("a", "V78", new DateOnly(2026, 9, 12)),
            new ContractorsReportApprovedVariation("b", "V80", new DateOnly(2026, 9, 12))
        };

        Assert.Equal("V78 and V80 were approved on 12 September 2026 and are no longer outstanding.",
            ContractorsReportVariationWording.ApprovedSince(approved));
    }

    private static async Task<JpmsContext> SeededContext()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"contractors-report-section-4-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France" });
        context.VariationOrders.AddRange(
            Variation(19, "Pond liner", VariationOrderStatus.Quoting),
            Variation(83, "Extended Preliminaries, EOT-05 (10 weeks)", VariationOrderStatus.Issued, issuedAt: new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero)),
            Variation(84, "Karndean out-of-sequence installation", VariationOrderStatus.Approved, approvedAt: new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero)),
            Variation(78, "Approved before the period", VariationOrderStatus.Approved, approvedAt: new DateTimeOffset(2026, 9, 12, 9, 0, 0, TimeSpan.Zero)));
        context.ContractorsReports.Add(new ContractorsReportEntity
        {
            ContractorsReportId = "report-31", ProjectId = Project, Number = 31,
            PeriodStart = Week.Start, PeriodEnd = Week.End, DateOfIssue = Week.End.AddDays(1)
        });
        await context.SaveChangesAsync();
        return context;
    }

    private static VariationOrderEntity Variation(int number, string title, VariationOrderStatus status,
        DateTimeOffset? issuedAt = null, DateTimeOffset? approvedAt = null) => new()
    {
        VariationOrderId = $"v{number}", ProjectId = Project, Number = number, Title = title,
        Status = (int)status, IssuedAt = issuedAt, ApprovedAt = approvedAt
    };
}
