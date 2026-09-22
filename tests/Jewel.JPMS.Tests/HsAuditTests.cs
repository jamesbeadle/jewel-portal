using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Hs.Audits;
using Jewel.JPMS.Api.Features.Hs.Audits.Commands;
using Jewel.JPMS.Api.Features.Hs.Commands;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The H&S site audit (2026-09-15, Katy-Louise's workbook brought into the portal): the score is
// her spreadsheet's figure to the percent, the framework plants whole, Issue mints one corrective
// action per finding and links it back, Close waits for the register, and a closed action stamps
// its item's date rectified.
public sealed class HsAuditTests
{
    private const string ByFrance = "3490f944b29545c4b8d5a04130f42ab8";
    private const string Officer = "katy-louise.hicks@jewelbb.co.uk";

    private static HsAuditItem Item(string code, HsAuditRate? rate, int minus = 0, string owner = "", HsAuditComment? comment = null, HsAuditClass? hsAuditClass = null) =>
        new(code, "audit", code, 1, code, comment, rate, hsAuditClass, minus, null, "", owner, null, null);

    // ---- Scoring: the spreadsheet's formula, pinned on the By France audit of 1 Sep 2026 ----

    [Fact]
    public void Score_isTheSpreadsheets_onByFrance()
    {
        // The 1 Sep audit as it stands on 22 Sep (3.04 and 11.01 unscored): 31 up to date, 5 a
        // week out of date, the F10 not in place, 144 unrated — rate average 325 / 360 = 0.9028 —
        // less the classes present: one C (the F10) and four Ds, each counted once: 0.06.
        var items = Enumerable.Range(0, 31).Select(i => Item($"u{i}", HsAuditRate.UpToDate, hsAuditClass: HsAuditClass.E))
            .Concat(Enumerable.Range(0, 4).Select(i => Item($"d{i}", HsAuditRate.OneWeekOutOfDate, hsAuditClass: HsAuditClass.D)))
            .Append(Item("scaffold", HsAuditRate.OneWeekOutOfDate, hsAuditClass: HsAuditClass.E))
            .Append(Item("f10", HsAuditRate.NotInPlace, hsAuditClass: HsAuditClass.C))
            .Concat(Enumerable.Range(0, 144).Select(i => Item($"n{i}", null)))
            .ToList();

        var score = HsAuditScoring.ScoreOf(items);

        Assert.Equal(0.9028m, HsAuditScoring.RateAverageOf(items));
        Assert.Equal(0.8428m, score);
        Assert.Equal("84%", HsAuditScoring.PercentText(score!.Value));
        Assert.Equal(HsAuditRating.Fair, HsAuditScoring.RatingOf(score.Value));
    }

    [Fact]
    public void Score_isBlankUntilAnItemIsRated_andNeverBelowZero()
    {
        Assert.Null(HsAuditScoring.ScoreOf(new[] { Item("a", null), Item("b", null) }));
        var everyClassAndARepeat = new[]
        {
            Item("a", HsAuditRate.NotInPlace, hsAuditClass: HsAuditClass.A, comment: HsAuditComment.Repeat),
            Item("b", HsAuditRate.NotInPlace, hsAuditClass: HsAuditClass.B),
            Item("c", HsAuditRate.NotInPlace, hsAuditClass: HsAuditClass.C),
            Item("d", HsAuditRate.NotInPlace, hsAuditClass: HsAuditClass.D)
        };
        Assert.Equal(0.51m, HsAuditClassPenalties.TotalFor(everyClassAndARepeat));
        Assert.Equal(0m, HsAuditScoring.ScoreOf(everyClassAndARepeat));
    }

    [Fact]
    public void Penalty_isChargedOncePerClassPresent_notPerFinding_andMinusIsNotInTheScore()
    {
        var twoDs = new[]
        {
            Item("a", HsAuditRate.UpToDate, minus: 25, hsAuditClass: HsAuditClass.D),
            Item("b", HsAuditRate.OneWeekOutOfDate, hsAuditClass: HsAuditClass.D)
        };

        Assert.Equal(0.01m, HsAuditClassPenalties.TotalFor(twoDs));
        Assert.Equal(0.74m, HsAuditScoring.ScoreOf(twoDs));
        Assert.Equal(0.1m, HsAuditClassPenalties.PointsOff(twoDs[0]));
        Assert.Equal(0.05m, HsAuditClassPenalties.TotalFor(new[] { Item("r", HsAuditRate.UpToDate, comment: HsAuditComment.Repeat) }));
    }

    [Theory]
    [InlineData(0.69, HsAuditRating.Poor)]
    [InlineData(0.70, HsAuditRating.Fair)]
    [InlineData(0.849, HsAuditRating.Fair)]
    [InlineData(0.85, HsAuditRating.Good)]
    [InlineData(0.95, HsAuditRating.VeryGood)]
    public void Rating_followsTheSpreadsheetsBands(double score, HsAuditRating expected) =>
        Assert.Equal(expected, HsAuditScoring.RatingOf((decimal)score));

    // ---- The framework ----

    [Fact]
    public void Template_isTheWorkbook_itemForItem()
    {
        Assert.Equal("2026-09-15", HsAuditTemplate.Version);
        Assert.Equal(11, HsAuditTemplate.Sections.Count);
        Assert.Equal(165, HsAuditTemplate.Items.Count);
        Assert.Equal(51, HsAuditTemplate.Items.Count(item => item.Section == 7));
        Assert.Equal("10.02", HsAuditTemplate.Items.Single(item => item.Name.StartsWith("Fire Points")).Code);
        Assert.Equal(HsAuditTemplate.Items.Count, HsAuditTemplate.Items.Select(item => item.Code).Distinct().Count());
        Assert.Equal("3.01", HsAuditTemplate.Items.Single(item => item.Name == "F10 - HSE Notification").Code);
        Assert.All(HsAuditTemplate.Items, item => Assert.Contains(HsAuditTemplate.Sections, section => section.Number == item.Section));
    }

    [Fact]
    public void Lineage_findsTheFirstVersionsItemOnTheCurrentOne()
    {
        Assert.Equal("10.02", HsAuditItemLineage.CurrentCodeFor(HsAuditTemplate.FirstVersion, "10.03"));
        Assert.Equal("9.01", HsAuditItemLineage.CurrentCodeFor(HsAuditTemplate.FirstVersion, "9.05"));
        Assert.Equal("3.01", HsAuditItemLineage.CurrentCodeFor(HsAuditTemplate.FirstVersion, "3.01"));
        Assert.Null(HsAuditItemLineage.CurrentCodeFor(HsAuditTemplate.FirstVersion, "8.11"));
        Assert.Equal("10.03", HsAuditItemLineage.CurrentCodeFor(HsAuditTemplate.Version, "10.03"));
        Assert.All(HsAuditTemplate.Items, item => Assert.Equal(item.Code, HsAuditItemLineage.CurrentCodeFor(HsAuditTemplate.Version, item.Code)));
    }

    [Fact]
    public async Task Create_plantsEveryItem_numbersPerProject_andCarriesThePreviousScore()
    {
        await using var context = NewContext();
        context.Projects.Add(new ProjectEntity { ProjectId = ByFrance, Name = "By France" });
        context.Projects.Add(new ProjectEntity { ProjectId = "other", Name = "Abbot Road" });
        await context.SaveChangesAsync();

        var first = await new CreateHsAuditHandler(context).HandleAsync(Create(ByFrance), CancellationToken.None);
        var elsewhere = await new CreateHsAuditHandler(context).HandleAsync(Create("other"), CancellationToken.None);

        Assert.Equal("HSA-0001", first.Reference);
        Assert.Equal("HSA-0001", elsewhere.Reference);
        Assert.Equal(HsAuditStatus.Draft, first.Status);
        Assert.Equal(HsAuditTemplate.Version, first.TemplateVersion);
        Assert.Null(first.PreviousScore);
        Assert.Equal(165, await context.HsAuditItems.CountAsync(item => item.HsAuditId == first.HsAuditId));

        await RateEverything(context, first.HsAuditId, HsAuditRate.UpToDate);
        await new IssueHsAuditHandler(context).HandleAsync(new IssueHsAudit(first.HsAuditId, Officer), CancellationToken.None);
        var second = await new CreateHsAuditHandler(context).HandleAsync(Create(ByFrance), CancellationToken.None);

        Assert.Equal("HSA-0002", second.Reference);
        Assert.Equal(1m, second.PreviousScore);
    }

    // ---- Issue: one corrective action per finding, linked back ----

    [Fact]
    public async Task Issue_mintsOneCorrectiveActionPerFinding_andLinksIt()
    {
        await using var context = NewContext();
        context.Projects.Add(new ProjectEntity { ProjectId = ByFrance, Name = "By France" });
        await context.SaveChangesAsync();
        var audit = await new CreateHsAuditHandler(context).HandleAsync(Create(ByFrance), CancellationToken.None);
        var items = await context.HsAuditItems.Where(item => item.HsAuditId == audit.HsAuditId).ToDictionaryAsync(item => item.Code);

        await new UpdateHsAuditItemsHandler(context).HandleAsync(new UpdateHsAuditItems(audit.HsAuditId, new[]
        {
            Entry(items["1.01"], HsAuditRate.UpToDate, HsAuditClass.E),
            Entry(items["1.14"], HsAuditRate.OneWeekOutOfDate, HsAuditClass.D, owner: "JE", findings: "No recent - one needs to be done in Sept", timeScale: HsAuditTimeScale.WithinOneMonth),
            Entry(items["3.01"], HsAuditRate.NotInPlace, HsAuditClass.C, owner: "KLH", findings: "Ran out 6/5/25", timeScale: HsAuditTimeScale.WithinSevenDays),
            Entry(items["3.04"], HsAuditRate.UpToDate, HsAuditClass.D, owner: "KLH"),
            Entry(items["8.01"], HsAuditRate.OneWeekOutOfDate, HsAuditClass.E, findings: "No scaff tags"),
            Entry(items["11.01"], HsAuditRate.OneWeekOutOfDate, HsAuditClass.D, comment: HsAuditComment.NotApplicable),
            Entry(items["7.03"], null, null, comment: HsAuditComment.NotSeen)
        }), CancellationToken.None);

        var issued = await new IssueHsAuditHandler(context).HandleAsync(new IssueHsAudit(audit.HsAuditId, Officer), CancellationToken.None);

        Assert.Equal(HsAuditStatus.Issued, issued.Audit.Status);
        Assert.NotNull(issued.Audit.IssuedAt);
        var actions = await context.HsRecords.Where(record => record.ProjectId == ByFrance).ToListAsync();
        // 1.14 (owner, rate 5), 3.01 (owner, rate 0), 3.04 (owner, rate 10), 8.01 (rate 5, no owner);
        // not 1.01 (full marks), not 11.01 (N/A), not 7.03 (unrated, no owner).
        Assert.Equal(4, actions.Count);
        Assert.All(actions, action => Assert.Equal((int)HsRecordKind.CorrectiveAction, action.Kind));
        Assert.All(actions, action => Assert.Equal((int)HsStatus.Open, action.Status));

        var f10 = actions.Single(action => action.Summary.StartsWith("3.01 F10"));
        Assert.Equal("KLH", f10.AssignedToName);
        Assert.Equal("", f10.AssignedToEmail);
        Assert.Equal((int)HsSeverity.Medium, f10.Severity);
        Assert.Equal(issued.Audit.IssuedAt!.Value.UtcDateTime.Date.AddDays(7), f10.DueAt!.Value.UtcDateTime);
        Assert.Equal(f10.HsRecordId, issued.Items.Single(item => item.Code == "3.01").HsRecordId);

        var scaffold = actions.Single(action => action.Summary.StartsWith("8.01 Scaffolding"));
        Assert.Equal("", scaffold.AssignedToName);
        Assert.Equal((int)HsSeverity.Low, scaffold.Severity);
        Assert.Null(scaffold.DueAt);
        Assert.Null(issued.Items.Single(item => item.Code == "1.01").HsRecordId);
    }

    [Fact]
    public async Task Issue_refusesAnUnratedAudit_andASecondIssue()
    {
        await using var context = NewContext();
        context.Projects.Add(new ProjectEntity { ProjectId = ByFrance, Name = "By France" });
        await context.SaveChangesAsync();
        var audit = await new CreateHsAuditHandler(context).HandleAsync(Create(ByFrance), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IssueHsAuditHandler(context).HandleAsync(new IssueHsAudit(audit.HsAuditId, Officer), CancellationToken.None));

        await RateEverything(context, audit.HsAuditId, HsAuditRate.UpToDate);
        await new IssueHsAuditHandler(context).HandleAsync(new IssueHsAudit(audit.HsAuditId, Officer), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IssueHsAuditHandler(context).HandleAsync(new IssueHsAudit(audit.HsAuditId, Officer), CancellationToken.None));
    }

    // ---- Close: the register has to agree ----

    [Fact]
    public async Task Close_waitsForEveryMintedAction_andAClosedActionStampsItsItem()
    {
        await using var context = NewContext();
        context.Projects.Add(new ProjectEntity { ProjectId = ByFrance, Name = "By France" });
        await context.SaveChangesAsync();
        var audit = await new CreateHsAuditHandler(context).HandleAsync(Create(ByFrance), CancellationToken.None);
        var fireBell = await context.HsAuditItems.SingleAsync(item => item.HsAuditId == audit.HsAuditId && item.Code == "10.03");
        await new UpdateHsAuditItemsHandler(context).HandleAsync(new UpdateHsAuditItems(audit.HsAuditId, new[]
        {
            Entry(fireBell, HsAuditRate.OneWeekOutOfDate, HsAuditClass.D, owner: "JE", findings: "Fire bell had come off")
        }), CancellationToken.None);
        var issued = await new IssueHsAuditHandler(context).HandleAsync(new IssueHsAudit(audit.HsAuditId, Officer), CancellationToken.None);
        var actionId = issued.Items.Single(item => item.Code == "10.03").HsRecordId!;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CloseHsAuditHandler(context).HandleAsync(new CloseHsAudit(audit.HsAuditId, "James Everitt"), CancellationToken.None));

        var action = await context.HsRecords.SingleAsync(record => record.HsRecordId == actionId);
        await new UpdateHsRecordHandler(context).HandleAsync(
            new UpdateHsRecord(actionId, action.Summary, HsSeverity.Low, HsStatus.Closed, "", null, "JE"), CancellationToken.None);

        var rectified = await context.HsAuditItems.AsNoTracking().SingleAsync(item => item.HsAuditItemId == fireBell.HsAuditItemId);
        Assert.NotNull(rectified.DateRectified);

        var closed = await new CloseHsAuditHandler(context).HandleAsync(new CloseHsAudit(audit.HsAuditId, "James Everitt"), CancellationToken.None);
        Assert.Equal(HsAuditStatus.Closed, closed.Status);
        Assert.Equal("James Everitt", closed.ManagerName);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdateHsAuditItemsHandler(context).HandleAsync(new UpdateHsAuditItems(audit.HsAuditId, new[]
            {
                Entry(fireBell, HsAuditRate.UpToDate, HsAuditClass.E)
            }), CancellationToken.None));
    }

    // ---- Helpers ----

    private static CreateHsAudit Create(string projectId) => new(
        projectId,
        new HsAuditDetails(HsAuditType.Routine, new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            "James Everitt", "Katy-Louise Hicks", "", null, ""),
        Officer);

    private static HsAuditItemEntry Entry(
        HsAuditItemEntity item, HsAuditRate? rate, HsAuditClass? hsAuditClass,
        string owner = "", string findings = "", HsAuditTimeScale? timeScale = null, HsAuditComment? comment = null) =>
        new(item.HsAuditItemId, comment, rate, hsAuditClass, 0, timeScale, findings, owner, null);

    private static async Task RateEverything(JpmsContext context, string hsAuditId, HsAuditRate rate)
    {
        var items = await context.HsAuditItems.Where(item => item.HsAuditId == hsAuditId).ToListAsync();
        await new UpdateHsAuditItemsHandler(context).HandleAsync(
            new UpdateHsAuditItems(hsAuditId, items.Select(item => Entry(item, rate, HsAuditClass.E)).ToList()), CancellationToken.None);
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"hs-audit-{Guid.NewGuid():N}").Options);
}
