using Jewel.JPMS.Api.Features.Progress.ContractorsReports;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;
using Jewel.JPMS.Contracts.Progress;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>The pure rules behind the weekly Contractor's Report: Section 1's day shape, the
/// wording gate, the Valuation No. default and the file name.</summary>
public class ContractorsReportTests
{
    private static readonly ReportingWeek Week = ReportingWeek.EndingOn(new DateOnly(2026, 9, 10));

    private static ContractorsReportUpdate UpdateOn(DateOnly day, string title = "Site notes") =>
        new("u-" + day.DayNumber, day, title, "", Array.Empty<ContractorsReportPhoto>());

    [Fact]
    public void Days_areFridayThenMondayToThursday_withTheWeekendFoldedIntoFriday_andEmptyDaysLeftOut()
    {
        var updates = new[]
        {
            UpdateOn(new DateOnly(2026, 9, 4), "Friday"),
            UpdateOn(new DateOnly(2026, 9, 5), "Saturday"),
            UpdateOn(new DateOnly(2026, 9, 8), "Tuesday")
        };

        var days = ContractorsReportDays.Group(Week, updates);

        Assert.Equal(2, days.Count);
        Assert.Equal(DayOfWeek.Friday, days[0].Date.DayOfWeek);
        Assert.Equal("Friday 4 September 2026", days[0].Heading);
        Assert.Equal("Friday, Saturday", string.Join(", ", days[0].Entries.Select(entry => entry.Title)));
        Assert.Equal(DayOfWeek.Tuesday, days[1].Date.DayOfWeek);
        Assert.Single(days[1].Entries);
    }

    [Fact]
    public void Wording_refusesABannedWord_namingTheSectionAndTheLine()
    {
        var findings = ContractorsReportWording.Check(new[]
        {
            (ContractorsReportSections.Progress, "Roof felt laid to the rear slope."),
            (ContractorsReportSections.LookAhead, "Snagging to the first-floor bathrooms"),
            (ContractorsReportSections.Neighbours, "Making good to the party wall was agreed with No. 14.")
        });

        Assert.Equal(2, findings.Count);
        Assert.Equal(ContractorsReportSections.LookAhead, findings[0].Section);
        Assert.Contains("snagging", findings[0].Reason);
        Assert.Equal(ContractorsReportSections.Neighbours, findings[1].Section);
        Assert.Contains("making good", findings[1].Reason);
    }

    [Fact]
    public void Wording_matchesWholeWordsOnly()
    {
        var findings = ContractorsReportWording.Check(new[] { (ContractorsReportSections.Progress, "Framework reworked? No — the frame was fixed.") });
        Assert.Empty(findings);
    }

    [Fact]
    public void ValuationNumber_defaultsToTheHighestCertificate_numerically()
    {
        Assert.Equal("10", ContractorsReportCertificates.Highest(new[] { "9", "10", "2" }));
        Assert.Null(ContractorsReportCertificates.Highest(new[] { "", " " }));
    }

    [Fact]
    public void FileName_carriesTheReferenceNumberAndWeekEnding()
    {
        var header = new ContractorsReportHeader("By France", "JBB-2026-001", "Contractor's Report No. 30", "15", "15", "",
            new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 10), "", "", new DateOnly(2026, 9, 11));
        Assert.Equal("JBB-2026-001 - Contractor's Report No. 30 - w-e 10 Sept 2026.pdf", ContractorsReportFileNames.Pdf(header));
    }
}
