using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The client-retention schedule: 5% held on works complete until practical completion,
// half released at completion, the balance after the defects period — all calculated from
// the valuation figures, with only confirmed releases stored.
public sealed class RetentionScheduleTests
{
    private static readonly DateTimeOffset PracticalCompletion = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

    private static ProjectRetention Terms(
        DateTimeOffset? practicalCompletionAt = null,
        int defectsPeriodMonths = 12,
        DateTimeOffset? completionConfirmedAt = null,
        decimal completionAmount = 0m,
        DateTimeOffset? finalConfirmedAt = null,
        decimal finalAmount = 0m) =>
        new(
            ProjectRetentionId: "R1",
            ProjectId: "PRJ-1",
            RetentionPercent: 5m,
            CompletionReleasePercent: 2.5m,
            DefectsPeriodMonths: defectsPeriodMonths,
            PracticalCompletionAt: practicalCompletionAt,
            CompletionReleaseConfirmedAt: completionConfirmedAt,
            CompletionReleaseAmount: completionAmount,
            FinalReleaseConfirmedAt: finalConfirmedAt,
            FinalReleaseAmount: finalAmount);

    // The By France position: works complete £1,647,990.65 against a revised contract sum
    // of £1,996,192.58 — held mirrors the workbook's "Less Retention @ 5%" line.
    [Fact]
    public void Forecast_heldIsRetentionPercentOfWorksComplete_releasesForecastAgainstRevisedSum()
    {
        var schedule = RetentionSchedule.For(Terms(PracticalCompletion), 1_647_990.65m, 1_996_192.58m);

        Assert.Equal(82_399.5325m, schedule.HeldToDate);           // 5% x works complete
        Assert.Equal(0m, schedule.ReleasedToDate);                 // nothing confirmed yet
        Assert.Equal(82_399.5325m, schedule.Outstanding);

        // First moiety: 2.5% of the revised contract sum, due at practical completion.
        Assert.Equal(49_904.8145m, schedule.CompletionRelease.Amount);
        Assert.Equal(PracticalCompletion, schedule.CompletionRelease.DueOn);
        Assert.False(schedule.CompletionRelease.IsConfirmed);

        // Balance: the rest of the 5% pot, due 12 months after completion.
        Assert.Equal(49_904.8145m, schedule.FinalRelease.Amount);  // (5% - 2.5%) x revised sum
        Assert.Equal(PracticalCompletion.AddMonths(12), schedule.FinalRelease.DueOn);
        Assert.False(schedule.FinalRelease.IsConfirmed);
    }

    [Fact]
    public void Forecast_sixMonthDefectsPeriod_movesTheFinalDueDate()
    {
        var schedule = RetentionSchedule.For(
            Terms(PracticalCompletion, defectsPeriodMonths: 6), 1_000_000m, 1_000_000m);

        Assert.Equal(PracticalCompletion.AddMonths(6), schedule.FinalRelease.DueOn);
    }

    [Fact]
    public void Forecast_noPracticalCompletionDate_leavesDueDatesUnset()
    {
        var schedule = RetentionSchedule.For(Terms(practicalCompletionAt: null), 500_000m, 1_000_000m);

        Assert.Null(schedule.CompletionRelease.DueOn);
        Assert.Null(schedule.FinalRelease.DueOn);
        Assert.Equal(25_000m, schedule.HeldToDate); // held still reads: 5% x works complete
    }

    [Fact]
    public void ConfirmedCompletionRelease_freezesItsAmount_andReducesOutstanding()
    {
        var confirmedAt = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var terms = Terms(PracticalCompletion, completionConfirmedAt: confirmedAt, completionAmount: 49_904.81m);

        // Works are fully complete, so held = 5% of the revised sum.
        var schedule = RetentionSchedule.For(terms, 1_996_192.58m, 1_996_192.58m);

        Assert.True(schedule.CompletionRelease.IsConfirmed);
        Assert.Equal(49_904.81m, schedule.CompletionRelease.Amount); // frozen, not recalculated
        Assert.Equal(confirmedAt, schedule.CompletionRelease.ConfirmedAt);
        Assert.Equal(49_904.81m, schedule.ReleasedToDate);
        Assert.Equal(99_809.629m - 49_904.81m, schedule.Outstanding); // held minus the freed moiety

        // The final release forecast is the pot less the FROZEN completion amount.
        Assert.Equal(99_809.629m - 49_904.81m, schedule.FinalRelease.Amount);
        Assert.False(schedule.FinalRelease.IsConfirmed);
    }

    [Fact]
    public void BothReleasesConfirmed_outstandingReachesZero()
    {
        var terms = Terms(
            PracticalCompletion,
            completionConfirmedAt: PracticalCompletion.AddDays(2), completionAmount: 49_904.81m,
            finalConfirmedAt: PracticalCompletion.AddMonths(12).AddDays(3), finalAmount: 49_904.82m);

        var schedule = RetentionSchedule.For(terms, 1_996_192.58m, 1_996_192.58m);

        Assert.Equal(99_809.63m, schedule.ReleasedToDate);
        Assert.Equal(99_809.629m - 99_809.63m, schedule.Outstanding); // ~0 (rounding on release)
        Assert.True(schedule.CompletionRelease.IsConfirmed);
        Assert.True(schedule.FinalRelease.IsConfirmed);
    }
}
