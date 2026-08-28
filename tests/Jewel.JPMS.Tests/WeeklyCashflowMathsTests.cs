using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Weekly Cashflow's arithmetic. The invariant everything leans on: every entry that goes in
// comes out in exactly one bucket, so the grid's totals always explain the inputs — moving an
// entry changes WHEN, never HOW MUCH. The other rules pinned here: weeks are Monday-anchored,
// overdue lands in the current week (never a past week, never dropped), a placement is the
// accountant's word and always wins (clamped into the axis), and recurring occurrences exist
// only inside the visible axis while a one-off waits in the current week however old it is.
public sealed class WeeklyCashflowMathsTests
{
    // A Thursday. Its week starts Monday 24 Aug 2026.
    private static readonly DateTimeOffset Today = new(2026, 8, 27, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WeekStart = new(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);

    private static WeeklyCashflowSeed Bill(string id, decimal amount, DateTimeOffset? due) =>
        new(WeeklyCashflowMaths.BillKeyFor(id), WeeklyCashflowBand.SupplierBills, "Supplier", id, amount, due);

    private static WeeklyCashflowItem Item(
        string id, decimal amount, WeeklyCashflowRecurrence recurrence,
        DateTimeOffset firstDueOn, DateTimeOffset? lastDueOn = null,
        WeeklyCashflowCategory category = WeeklyCashflowCategory.Subcontractor,
        DateTimeOffset? archivedAt = null) =>
        new(id, "Item " + id, category, amount, recurrence, firstDueOn, lastDueOn,
            null, "fd@jewelbb.co.uk", Today, archivedAt);

    private static WeeklyCashflowView Build(
        IReadOnlyList<WeeklyCashflowSeed>? seeds = null,
        IReadOnlyList<WeeklyCashflowItem>? items = null,
        IReadOnlyList<WeeklyCashflowPlacement>? placements = null,
        decimal? opening = null) =>
        WeeklyCashflowMaths.Build(
            Today,
            seeds ?? Array.Empty<WeeklyCashflowSeed>(),
            items ?? Array.Empty<WeeklyCashflowItem>(),
            placements ?? Array.Empty<WeeklyCashflowPlacement>(),
            opening);

    // ---- The week axis -------------------------------------------------------------------

    [Theory]
    [InlineData(2026, 8, 24)] // Monday
    [InlineData(2026, 8, 27)] // Thursday
    [InlineData(2026, 8, 30)] // Sunday — still the same week
    public void WeekStartFor_isTheMonday(int year, int month, int day)
    {
        var start = WeeklyCashflowMaths.WeekStartFor(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal(WeekStart, start);
    }

    [Fact]
    public void BuildWeeks_runsThirteenMondaysFromTheCurrentWeek()
    {
        var weeks = WeeklyCashflowMaths.BuildWeeks(Today);
        Assert.Equal(WeeklyCashflowMaths.DefaultWeekCount, weeks.Count);
        Assert.Equal(WeekStart, weeks[0]);
        Assert.All(weeks, week => Assert.Equal(DayOfWeek.Monday, week.UtcDateTime.DayOfWeek));
        Assert.Equal(WeekStart.AddDays(7 * 12), weeks[^1]);
    }

    // ---- Seeding by due date -------------------------------------------------------------

    [Fact]
    public void DueDates_landInTheirWeek_overdueAndUndatedLandNow_beyondTheAxisLandsLater()
    {
        var view = Build(seeds: new[]
        {
            Bill("overdue", 100m, Today.AddDays(-40)),
            Bill("undated", 50m, null),
            Bill("thisWeek", 10m, Today.AddDays(1)),
            Bill("nextWeek", 20m, WeekStart.AddDays(7)),
            Bill("beyond", 30m, WeekStart.AddDays(7 * 20))
        });

        Assert.Equal(0, view.Entries.Single(e => e.PlacementKey == "bill:overdue").WeekIndex);
        Assert.Equal(0, view.Entries.Single(e => e.PlacementKey == "bill:undated").WeekIndex);
        Assert.Equal(0, view.Entries.Single(e => e.PlacementKey == "bill:thisWeek").WeekIndex);
        Assert.Equal(1, view.Entries.Single(e => e.PlacementKey == "bill:nextWeek").WeekIndex);
        Assert.Equal(view.LaterIndex, view.Entries.Single(e => e.PlacementKey == "bill:beyond").WeekIndex);
        Assert.All(view.Entries, entry => Assert.False(entry.Moved));
    }

    // ---- Placements — the accountant's word ----------------------------------------------

    [Fact]
    public void APlacement_overridesTheDueDate_andMarksTheEntryMoved()
    {
        var view = Build(
            seeds: new[] { Bill("b1", 100m, Today.AddDays(1)) },
            placements: new[]
            {
                new WeeklyCashflowPlacement("bill:b1", WeekStart.AddDays(21), "acc@jewelbb.co.uk", Today)
            });

        var entry = view.Entries.Single();
        Assert.Equal(3, entry.WeekIndex);
        Assert.True(entry.Moved);
    }

    [Fact]
    public void AStalePlacement_inAPastWeek_meansPayNow()
    {
        var view = Build(
            seeds: new[] { Bill("b1", 100m, WeekStart.AddDays(35)) },
            placements: new[]
            {
                new WeeklyCashflowPlacement("bill:b1", WeekStart.AddDays(-14), "acc@jewelbb.co.uk", Today)
            });

        Assert.Equal(0, view.Entries.Single().WeekIndex);
    }

    [Fact]
    public void APlacementBeyondTheAxis_landsInLater()
    {
        var view = Build(
            seeds: new[] { Bill("b1", 100m, Today) },
            placements: new[]
            {
                new WeeklyCashflowPlacement("bill:b1", WeekStart.AddDays(7 * 30), "acc@jewelbb.co.uk", Today)
            });

        Assert.Equal(view.LaterIndex, view.Entries.Single().WeekIndex);
    }

    [Fact]
    public void APlacementForAnEntryThatIsGone_isSimplyNeverAskedFor()
    {
        var view = Build(
            seeds: new[] { Bill("b1", 100m, Today) },
            placements: new[]
            {
                new WeeklyCashflowPlacement("bill:paid-long-ago", WeekStart.AddDays(7), "acc@jewelbb.co.uk", Today)
            });

        Assert.Single(view.Entries);
        Assert.Equal(100m, view.CashOut.Sum());
    }

    // ---- Manual items: one-offs ----------------------------------------------------------

    [Fact]
    public void AOneOff_staysInTheCurrentWeek_howeverOldItIs()
    {
        var view = Build(items: new[]
        {
            Item("old", 500m, WeeklyCashflowRecurrence.OneOff, Today.AddDays(-90))
        });

        var entry = view.Entries.Single();
        Assert.Equal(0, entry.WeekIndex);
        Assert.Equal("old", entry.ItemId);
    }

    [Fact]
    public void AOneOffBeyondTheHorizon_waitsInLater()
    {
        var view = Build(items: new[]
        {
            Item("far", 500m, WeeklyCashflowRecurrence.OneOff, WeekStart.AddDays(7 * 26))
        });

        Assert.Equal(view.LaterIndex, view.Entries.Single().WeekIndex);
    }

    [Fact]
    public void AnArchivedItem_hasLeftTheGrid()
    {
        var view = Build(items: new[]
        {
            Item("gone", 500m, WeeklyCashflowRecurrence.OneOff, Today, archivedAt: Today)
        });

        Assert.Empty(view.Entries);
    }

    // ---- Manual items: recurring ---------------------------------------------------------

    [Fact]
    public void AWeeklyItem_fillsEveryWeekFromItsFirstDate_onItsWeekday()
    {
        var firstDue = WeekStart.AddDays(14 + 4); // a Friday, two weeks out
        var view = Build(items: new[]
        {
            Item("wages", 1_000m, WeeklyCashflowRecurrence.Weekly, firstDue)
        });

        Assert.Equal(11, view.Entries.Count); // weeks 2..12 — nothing in Later
        Assert.Equal(Enumerable.Range(2, 11), view.Entries.Select(e => e.WeekIndex).OrderBy(i => i));
        Assert.All(view.Entries, entry => Assert.Equal(DayOfWeek.Friday, entry.NaturalDueOn!.Value.UtcDateTime.DayOfWeek));
    }

    [Fact]
    public void AWeeklyItemFromThePast_startsThisWeek_pastOccurrencesAreAssumedDealtWith()
    {
        var view = Build(items: new[]
        {
            Item("wages", 1_000m, WeeklyCashflowRecurrence.Weekly, Today.AddDays(-70))
        });

        Assert.Equal(WeeklyCashflowMaths.DefaultWeekCount, view.Entries.Count);
        Assert.Equal(0, view.Entries.Min(e => e.WeekIndex));
    }

    [Fact]
    public void AWeeklyItem_stopsAtItsLastDate()
    {
        var view = Build(items: new[]
        {
            Item("wages", 1_000m, WeeklyCashflowRecurrence.Weekly, WeekStart, lastDueOn: WeekStart.AddDays(21))
        });

        Assert.Equal(4, view.Entries.Count); // weeks 0..3 inclusive
    }

    [Fact]
    public void AMonthlyItem_recursOnItsDay_andClampsIntoShorterMonths()
    {
        // Anchored on the 31st from 31 Aug: Sep has 30 days, so September's occurrence is the 30th.
        var view = Build(items: new[]
        {
            Item("subscription", 99m, WeeklyCashflowRecurrence.Monthly,
                new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
                category: WeeklyCashflowCategory.Subscription)
        });

        var dates = view.Entries.Select(e => e.NaturalDueOn!.Value.UtcDateTime).OrderBy(d => d).ToList();
        Assert.Equal(new DateTime(2026, 8, 31), dates[0]);
        Assert.Equal(new DateTime(2026, 9, 30), dates[1]);
        Assert.Equal(new DateTime(2026, 10, 31), dates[2]);
        Assert.All(view.Entries, entry => Assert.True(entry.WeekIndex < view.LaterIndex));
    }

    [Fact]
    public void ARecurringOccurrence_movesOnItsOwn_theRestOfTheScheduleStaysPut()
    {
        var firstDue = WeekStart.AddDays(7); // weekly from next week
        var items = new[] { Item("wages", 1_000m, WeeklyCashflowRecurrence.Weekly, firstDue) };
        var movedKey = WeeklyCashflowMaths.ManualKeyFor("wages", firstDue);

        var view = Build(
            items: items,
            placements: new[] { new WeeklyCashflowPlacement(movedKey, WeekStart.AddDays(28), "acc@jewelbb.co.uk", Today) });

        var moved = view.Entries.Single(e => e.PlacementKey == movedKey);
        Assert.Equal(4, moved.WeekIndex);
        Assert.True(moved.Moved);
        Assert.All(view.Entries.Where(e => e.PlacementKey != movedKey), entry => Assert.False(entry.Moved));
    }

    // ---- The Xero expected/planned date --------------------------------------------------
    // The accountant's "Expected date" (invoices) / "Planned date" (bills) set in Xero is the
    // natural week when present — the due date stays what the document says is owed, and a
    // stored placement still outranks both.

    [Fact]
    public void AnExpectedDate_notTheDueDate_picksTheNaturalWeek()
    {
        var seed = Bill("b1", 500m, Today.AddDays(-90)) with { ExpectedOn = WeekStart.AddDays(7 * 3) };

        var view = Build(new[] { seed });

        var entry = Assert.Single(view.Entries);
        Assert.Equal(3, entry.WeekIndex);
        Assert.False(entry.Moved);                      // Xero's date is the natural week, not a move
        Assert.Equal(seed.DueOn, entry.NaturalDueOn);   // the due date survives for display
        Assert.Equal(seed.ExpectedOn, entry.NaturalOn); // and ↺ returns to the expected week
    }

    [Fact]
    public void APlacement_stillWins_overTheExpectedDate()
    {
        var seed = Bill("b1", 500m, Today.AddDays(3)) with { ExpectedOn = WeekStart.AddDays(7 * 3) };
        var placements = new[]
        {
            new WeeklyCashflowPlacement("bill:b1", WeekStart.AddDays(7 * 6), "acc@jewelbb.co.uk", Today)
        };

        var view = Build(new[] { seed }, placements: placements);

        var entry = Assert.Single(view.Entries);
        Assert.Equal(6, entry.WeekIndex);
        Assert.True(entry.Moved);
    }

    [Fact]
    public void AnOverdueExpectedDate_landsInTheCurrentWeek_likeAnyOverdueEntry()
    {
        var seed = Bill("b1", 500m, Today.AddDays(30)) with { ExpectedOn = Today.AddDays(-21) };

        var view = Build(new[] { seed });

        var entry = Assert.Single(view.Entries);
        Assert.Equal(0, entry.WeekIndex);
        Assert.False(entry.Moved);
    }

    // ---- The invariant: totals always explain the inputs ---------------------------------

    [Fact]
    public void TotalsAlwaysExplainTheInputs_movedOrNot()
    {
        var seeds = new[]
        {
            Bill("b1", 1_234.56m, Today.AddDays(3)),
            Bill("b2", -200m, Today.AddDays(10)), // a credit note
            new WeeklyCashflowSeed(WeeklyCashflowMaths.ReceiptKeyFor("i1"),
                WeeklyCashflowBand.ClientReceipts, "Client", "INV-1", 9_000m, Today.AddDays(17))
        };
        var items = new[] { Item("sub", 750m, WeeklyCashflowRecurrence.OneOff, Today.AddDays(20)) };
        var placements = new[]
        {
            new WeeklyCashflowPlacement("bill:b1", WeekStart.AddDays(7 * 9), "acc@jewelbb.co.uk", Today)
        };

        var view = Build(seeds, items, placements);

        Assert.Equal(1_234.56m - 200m + 750m, view.CashOut.Sum());
        Assert.Equal(9_000m, view.CashIn.Sum());
        for (var index = 0; index <= WeeklyCashflowMaths.DefaultWeekCount; index++)
            Assert.Equal(view.CashIn[index] - view.CashOut[index], view.Net[index]);
    }

    // ---- The balance line ----------------------------------------------------------------

    [Fact]
    public void TheClosingBalance_runsFromTheOpening_andFlagsTheLowestWeek()
    {
        var view = Build(
            seeds: new[]
            {
                Bill("b1", 600m, WeekStart.AddDays(7)),          // week 1
                new WeeklyCashflowSeed(WeeklyCashflowMaths.ReceiptKeyFor("i1"),
                    WeeklyCashflowBand.ClientReceipts, "Client", "INV-1", 900m, WeekStart.AddDays(14)) // week 2
            },
            opening: 1_000m);

        Assert.NotNull(view.Closing);
        Assert.Equal(1_000m, view.Closing![0]);
        Assert.Equal(400m, view.Closing[1]);
        Assert.Equal(1_300m, view.Closing[2]);
        Assert.Equal(1_300m, view.Closing[^1]);
        Assert.Equal(1, view.MinClosingIndex);
    }

    [Fact]
    public void NoOpeningBalance_noBalanceLine()
    {
        var view = Build(seeds: new[] { Bill("b1", 100m, Today) });
        Assert.Null(view.Closing);
        Assert.Equal(-1, view.MinClosingIndex);
    }

    // ---- Later stays out of the balance --------------------------------------------------

    [Fact]
    public void LaterEntries_countInTotals_butNotInTheBalance()
    {
        var view = Build(
            seeds: new[] { Bill("far", 500m, WeekStart.AddDays(7 * 40)) },
            opening: 1_000m);

        Assert.Equal(500m, view.CashOut[view.LaterIndex]);
        Assert.All(view.Closing!, balance => Assert.Equal(1_000m, balance));
    }
}
