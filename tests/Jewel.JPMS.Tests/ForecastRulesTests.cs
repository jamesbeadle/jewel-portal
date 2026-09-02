using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Forecast rules for the Labour overview (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md
/// §5): working-day arithmetic, contracted-days × day-rate projection net of absence, the CIS
/// net-payable line, and the week-signability check behind the sign-off marker.
/// </summary>
public class ForecastRulesTests
{
    // --- working-day arithmetic ---

    [Theory]
    [InlineData(2026, 8, 21)]  // August 2026: 21 weekdays
    [InlineData(2026, 2, 20)]  // February 2026: 20 weekdays
    [InlineData(2026, 5, 21)]  // May 2026 (bank holidays are still weekdays — not modelled)
    public void CountsMondayToFridayDaysInTheMonth(int year, int month, int expected) =>
        Assert.Equal(expected, ForecastRules.WorkingDaysInMonth(year, month));

    [Fact]
    public void ElapsedDaysAreZeroForAFutureMonth() =>
        Assert.Equal(0, ForecastRules.ElapsedWorkingDays(2026, 9, new DateTime(2026, 8, 18)));

    [Fact]
    public void ElapsedDaysCountUpToTodayInsideTheMonth() =>
        // 1..18 August 2026: 1st is a Saturday; 12 weekdays elapse by Tuesday the 18th.
        Assert.Equal(12, ForecastRules.ElapsedWorkingDays(2026, 8, new DateTime(2026, 8, 18)));

    [Fact]
    public void ElapsedDaysAreTheFullMonthOnceItHasPassed() =>
        Assert.Equal(ForecastRules.WorkingDaysInMonth(2026, 7),
            ForecastRules.ElapsedWorkingDays(2026, 7, new DateTime(2026, 8, 18)));

    // --- projection: contracted days × day rate, less absence at the day rate ---

    [Fact]
    public void ProjectionIsContractedDaysTimesDayRate() =>
        Assert.Equal(5000m, ForecastRules.ProjectedCost(20m, 250m, Array.Empty<AbsenceKind>()));

    [Fact]
    public void HolidayDeductsAFullDayAndHalfDayDeductsHalf()
    {
        var absences = new[] { AbsenceKind.Holiday, AbsenceKind.HalfDay };
        Assert.Equal(4625m, ForecastRules.ProjectedCost(20m, 250m, absences));
        Assert.Equal(375m, ForecastRules.TimeOffCost(250m, absences));
    }

    [Fact]
    public void ProjectionNeverGoesBelowZero() =>
        Assert.Equal(0m, ForecastRules.ProjectedCost(1m, 250m,
            new[] { AbsenceKind.Holiday, AbsenceKind.Holiday, AbsenceKind.Sick }));

    // --- CIS: amount due is the net payable, the reference build's 80% column made explicit ---

    [Theory]
    [InlineData(5000, 20, 4000)]   // standard deduction
    [InlineData(3623, 20, 2898.40)]
    [InlineData(5000, 30, 3500)]   // unverified
    [InlineData(5000, 0, 5000)]    // gross status
    public void AmountDueIsNetOfTheCisRate(decimal projected, decimal cisRate, decimal expected) =>
        Assert.Equal(expected, ForecastRules.AmountDue(projected, cisRate));

    [Fact]
    public void HoursConvertToDaysAtTheEightHourStandardDay()
    {
        Assert.Equal(1m, ForecastRules.HoursToDays(8m));
        Assert.Equal(0.5m, ForecastRules.HoursToDays(4m));
        Assert.Equal(1.31m, ForecastRules.HoursToDays(10.5m));
    }

    // --- week arithmetic & signability ---

    [Theory]
    [InlineData(2026, 8, 18, 2026, 8, 17)] // Tuesday → Monday
    [InlineData(2026, 8, 17, 2026, 8, 17)] // Monday → itself
    [InlineData(2026, 8, 23, 2026, 8, 17)] // Sunday → previous Monday
    public void WeekStartIsTheMonday(int y, int m, int d, int ey, int em, int ed) =>
        Assert.Equal(new DateTime(ey, em, ed), ForecastRules.WeekStartOf(new DateTime(y, m, d)));

    private static readonly DateTime Monday = new(2026, 8, 17);

    [Fact]
    public void WeekIsSignableWhenEveryElapsedDayIsSettledOrAbsent()
    {
        var settled = new HashSet<DateTime> { Monday, Monday.AddDays(1) };            // Mon, Tue approved
        var absent = new HashSet<DateTime> { Monday.AddDays(2) };                     // Wed on holiday
        Assert.True(ForecastRules.WeekIsSignable(Monday, Monday.AddDays(2), settled, absent));
    }

    [Fact]
    public void WeekIsNotSignableWithAnUnexplainedElapsedDay()
    {
        var settled = new HashSet<DateTime> { Monday };                               // Tue has nothing
        Assert.False(ForecastRules.WeekIsSignable(Monday, Monday.AddDays(2), settled, new HashSet<DateTime>()));
    }

    [Fact]
    public void FutureDaysDoNotBlockSignOff() =>
        // Today is the Monday itself: only Monday needs settling; Tue–Fri are future.
        Assert.True(ForecastRules.WeekIsSignable(Monday, Monday,
            new HashSet<DateTime> { Monday }, new HashSet<DateTime>()));

    // ---- Week parts (2026-09-02): the week of Mon 31 Aug 2026 runs to Sun 6 Sep -------------

    private static readonly DateTime StraddlingMonday = new(2026, 8, 31);
    private static readonly DateTime August = new(2026, 8, 1);
    private static readonly DateTime September = new(2026, 9, 1);

    [Fact]
    public void AWeekAcrossTheMonthEndStraddles_aWeekInsideOneMonthDoesNot()
    {
        Assert.True(ForecastRules.WeekStraddlesMonthEnd(StraddlingMonday));
        Assert.False(ForecastRules.WeekStraddlesMonthEnd(Monday));                 // 17–23 Aug
        Assert.True(ForecastRules.WeekStraddlesMonthEnd(new DateTime(2026, 12, 28))); // into January
    }

    [Fact]
    public void AWeekTouchesEveryMonthItHasADayIn()
    {
        Assert.True(ForecastRules.WeekTouchesMonth(StraddlingMonday, August));
        Assert.True(ForecastRules.WeekTouchesMonth(StraddlingMonday, September));
        Assert.False(ForecastRules.WeekTouchesMonth(StraddlingMonday, new DateTime(2026, 10, 1)));
        Assert.False(ForecastRules.WeekTouchesMonth(Monday, September));
    }

    [Fact]
    public void TheMonthPartOfAStraddlingWeekIsItsDaysOnThatSideOfTheMonthEnd()
    {
        Assert.Equal((StraddlingMonday, StraddlingMonday), ForecastRules.WeekPart(StraddlingMonday, August));      // 31 Aug only
        Assert.Equal((September, new DateTime(2026, 9, 6)), ForecastRules.WeekPart(StraddlingMonday, September)); // 1–6 Sep
        Assert.Equal((Monday, Monday.AddDays(6)), ForecastRules.WeekPart(Monday, August));                        // the whole week
    }

    [Fact]
    public void AugustsPartSignsOffOnTheFirst_withOnlyMondayApproved()
    {
        // 2 Sep: Tue and Wed have elapsed with nothing approved — September's business, not August's.
        var settled = new HashSet<DateTime> { StraddlingMonday };
        var today = new DateTime(2026, 9, 2);
        Assert.True(ForecastRules.WeekPartIsSignable(StraddlingMonday, August, today, settled, new HashSet<DateTime>()));
        Assert.False(ForecastRules.WeekPartIsSignable(StraddlingMonday, September, today, settled, new HashSet<DateTime>()));
        // The whole-week rule still wants the September days — that is exactly what held August up.
        Assert.False(ForecastRules.WeekIsSignable(StraddlingMonday, today, settled, new HashSet<DateTime>()));
    }

    [Fact]
    public void AugustsPartIsNotSignableWhileMondayItselfIsOpen() =>
        Assert.False(ForecastRules.WeekPartIsSignable(StraddlingMonday, August, new DateTime(2026, 9, 2),
            new HashSet<DateTime> { September, new DateTime(2026, 9, 2) }, new HashSet<DateTime>()));

    [Fact]
    public void SeptembersPartIgnoresAugustsMonday()
    {
        var settled = new HashSet<DateTime> { September, new DateTime(2026, 9, 2) };
        Assert.True(ForecastRules.WeekPartIsSignable(StraddlingMonday, September, new DateTime(2026, 9, 2), settled, new HashSet<DateTime>()));
    }

    [Fact]
    public void AWeekInsideOneMonthSignsByTheWholeWeekRule()
    {
        var settled = new HashSet<DateTime> { Monday, Monday.AddDays(1) };
        var absent = new HashSet<DateTime> { Monday.AddDays(2) };
        Assert.Equal(
            ForecastRules.WeekIsSignable(Monday, Monday.AddDays(2), settled, absent),
            ForecastRules.WeekPartIsSignable(Monday, August, Monday.AddDays(2), settled, absent));
        Assert.Equal(
            ForecastRules.WeekIsSignable(Monday, Monday.AddDays(3), settled, absent),
            ForecastRules.WeekPartIsSignable(Monday, August, Monday.AddDays(3), settled, absent));
    }
}
