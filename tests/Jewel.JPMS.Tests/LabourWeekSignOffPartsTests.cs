using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Sign-off is per worker, week AND month (2026-09-02, the accountant's ask): the week of
/// Mon 31 Aug 2026 has an August part (31 Aug) and a September part (1–6 Sep), and August's
/// Xero run waits for its own part only. These tests pin how a command names a part and how
/// the settlement gate reads the markers.
/// </summary>
public sealed class LabourWeekSignOffPartsTests
{
    private static readonly DateTimeOffset StraddlingMonday = new(new DateTime(2026, 8, 31), TimeSpan.Zero);
    private static readonly DateTimeOffset August = new(new DateTime(2026, 8, 1), TimeSpan.Zero);
    private static readonly DateTimeOffset September = new(new DateTime(2026, 9, 1), TimeSpan.Zero);

    // ---- Addressing a part -----------------------------------------------------------------

    [Fact]
    public void TheMonthDefaultsToTheMonthOfTheDateGiven()
    {
        // 31 Aug → August's part of the week; 1 Sep → September's part of the SAME week.
        Assert.Equal((StraddlingMonday, August), LabourWeekParts.Resolve(new DateTimeOffset(new DateTime(2026, 8, 31), TimeSpan.Zero), null));
        Assert.Equal((StraddlingMonday, September), LabourWeekParts.Resolve(new DateTimeOffset(new DateTime(2026, 9, 1), TimeSpan.Zero), null));
        Assert.Equal((StraddlingMonday, September), LabourWeekParts.Resolve(new DateTimeOffset(new DateTime(2026, 9, 4), TimeSpan.Zero), null));
    }

    [Fact]
    public void AnExplicitMonthWinsAndNormalisesToTheFirst()
    {
        var anyDayInAugust = new DateTimeOffset(new DateTime(2026, 8, 19), TimeSpan.Zero);
        Assert.Equal((StraddlingMonday, August), LabourWeekParts.Resolve(new DateTimeOffset(new DateTime(2026, 9, 3), TimeSpan.Zero), anyDayInAugust));
    }

    [Fact]
    public void AMonthTheWeekNeverTouchesIsRefused() =>
        Assert.Throws<InvalidOperationException>(() =>
            LabourWeekParts.Resolve(StraddlingMonday, new DateTimeOffset(new DateTime(2026, 10, 1), TimeSpan.Zero)));

    [Fact]
    public void ARefusalNamesThePartInPlainWords()
    {
        Assert.Equal("August's part of the week of 31 Aug (31 Aug)", LabourWeekParts.Describe(StraddlingMonday, August));
        Assert.Equal("September's part of the week of 31 Aug (1–6 Sep)", LabourWeekParts.Describe(StraddlingMonday, September));
        Assert.Equal("The week of 17 Aug", LabourWeekParts.Describe(new DateTimeOffset(new DateTime(2026, 8, 17), TimeSpan.Zero), August));
    }

    // ---- The settlement gate ---------------------------------------------------------------

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"sign-off-parts-{Guid.NewGuid():N}")
            .Options);

    private static async Task SeedJayAsync(JpmsContext db)
    {
        db.Workers.Add(new WorkerEntity { WorkerId = "W-JAY", Name = "Jay", HourlyRate = 25m, IsActive = true, IsSoleTrader = true });
        db.Projects.Add(new ProjectEntity { ProjectId = "P1", Reference = "JBB-2026-004", Name = "Woodhouse", ClientName = "David Needham" });
        // Approved time on Monday 31 August — the week that runs into September.
        db.Timesheets.Add(new TimesheetEntity
        {
            TimesheetId = "T-31AUG", ProjectId = "P1", WorkerId = "W-JAY", WorkedOn = StraddlingMonday,
            Hours = 8m, CostCode = "SUB-GWK", Status = (int)TimesheetStatus.Approved, IsApproved = true,
            RateApplied = 25m, CostAmount = 200m
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AugustIsFullySignedOffByItsOwnPartOfTheStraddlingWeek()
    {
        await using var db = NewContext();
        await SeedJayAsync(db);
        db.LabourWeekSignOffs.Add(new LabourWeekSignOffEntity
        {
            LabourWeekSignOffId = "S1", WorkerId = "W-JAY", WeekStart = StraddlingMonday, MonthStart = August,
            SignedOffByEmail = "accounts@jewelbb.co.uk", SignedOffAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var august = await new SettlementScheduleBuilder(db).BuildAsync(2026, 8, CancellationToken.None);

        var jay = Assert.Single(august.Workers);
        Assert.True(jay.FullySignedOff);
        Assert.Equal(200m, jay.GrossLabour);
    }

    [Fact]
    public async Task SeptembersPartOfTheWeekDoesNotSignAugustOff()
    {
        await using var db = NewContext();
        await SeedJayAsync(db);
        db.LabourWeekSignOffs.Add(new LabourWeekSignOffEntity
        {
            LabourWeekSignOffId = "S2", WorkerId = "W-JAY", WeekStart = StraddlingMonday, MonthStart = September,
            SignedOffByEmail = "accounts@jewelbb.co.uk", SignedOffAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var august = await new SettlementScheduleBuilder(db).BuildAsync(2026, 8, CancellationToken.None);

        Assert.False(Assert.Single(august.Workers).FullySignedOff);
    }

    [Fact]
    public async Task WithNoMarkerAugustStaysOpen()
    {
        await using var db = NewContext();
        await SeedJayAsync(db);

        var august = await new SettlementScheduleBuilder(db).BuildAsync(2026, 8, CancellationToken.None);

        Assert.False(Assert.Single(august.Workers).FullySignedOff);
    }
}
