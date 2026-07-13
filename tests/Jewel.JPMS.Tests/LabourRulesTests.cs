using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Labour tracking rules (docs/Labour-Time-Tracking-Scope.md): half-hour capture steps, the
/// rate-effective-on-worked-date resolution that gets snapshotted at approval, hours × rate
/// costing, and the budget hard-block from workflow 07-D.
/// </summary>
public class LabourRulesTests
{
    // --- hours validity: 0.5 steps, minimum 0.5 (spec constraint) ---

    [Theory]
    [InlineData(0.5, true)]
    [InlineData(2.5, true)]
    [InlineData(12.5, true)]  // overtime is valid — soft warning only, never blocked
    [InlineData(0, false)]
    [InlineData(0.25, false)]
    [InlineData(3.7, false)]
    [InlineData(-1, false)]
    public void HoursMustBeHalfHourStepsOfAtLeastHalfAnHour(decimal hours, bool expected) =>
        Assert.Equal(expected, LabourRules.IsValidHours(hours));

    // --- sign-out entry validation ---

    private static readonly IReadOnlySet<string> Codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "00010", "00020" };

    [Fact]
    public void SignOutNeedsAtLeastOneEntry() =>
        Assert.NotEmpty(LabourRules.CheckSignOutEntries(Array.Empty<SiteSignOutEntry>(), Codes));

    [Fact]
    public void SignOutAcceptsValidEntries() =>
        Assert.Empty(LabourRules.CheckSignOutEntries(
            new[] { new SiteSignOutEntry("00010", 3m), new SiteSignOutEntry("00020", 2.5m) }, Codes));

    [Fact]
    public void SignOutRejectsCostCodesOffTheProjectList() =>
        Assert.Contains(LabourRules.CheckSignOutEntries(
            new[] { new SiteSignOutEntry("99999", 3m) }, Codes),
            error => error.Contains("99999"));

    [Fact]
    public void SignOutRejectsDuplicateCostCodes() =>
        Assert.NotEmpty(LabourRules.CheckSignOutEntries(
            new[] { new SiteSignOutEntry("00010", 3m), new SiteSignOutEntry("00010", 2m) }, Codes));

    [Fact]
    public void SignOutRejectsQuarterHours() =>
        Assert.NotEmpty(LabourRules.CheckSignOutEntries(
            new[] { new SiteSignOutEntry("00010", 3.25m) }, Codes));

    // --- rate resolution: effective on the worked date, snapshotted at approval ---

    private static readonly DateTimeOffset Jan1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Jun1 = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RateEffectiveOnWorkedDateWins()
    {
        var history = new List<(DateTimeOffset, decimal)> { (Jan1, 25m), (Jun1, 27.5m) };
        Assert.Equal(25m, LabourRules.ResolveRate(history, 27.5m, new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero)));
        Assert.Equal(27.5m, LabourRules.ResolveRate(history, 27.5m, new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void RateFallsBackToCurrentWhenWorkedBeforeAnyHistory()
    {
        var history = new List<(DateTimeOffset, decimal)> { (Jun1, 27.5m) };
        Assert.Equal(26m, LabourRules.ResolveRate(history, 26m, Jan1));
    }

    [Fact]
    public void RateChangeOnTheWorkedDayApplies()
    {
        var history = new List<(DateTimeOffset, decimal)> { (Jan1, 25m), (Jun1, 27.5m) };
        Assert.Equal(27.5m, LabourRules.ResolveRate(history, 27.5m, Jun1));
    }

    // --- costing: hours × rate, £200 day rate ÷ 8 example from the scope ---

    [Fact]
    public void CostIsHoursTimesRate()
    {
        var hourly = 200m / 8m; // £200 day rate, standard 8-hour day
        Assert.Equal(75m, LabourRules.CostOf(3m, hourly));
        Assert.Equal(250m, LabourRules.CostOf(10m, hourly)); // a 10-hour day costs more than the day rate — hourly model, per the scope decision
    }

    [Fact]
    public void CostRoundsToPennies() =>
        Assert.Equal(8.33m, LabourRules.CostOf(0.5m, 16.666m));

    // --- budget hard-block (workflow 07-D) ---

    [Fact]
    public void NoBudgetRowBlocksApproval() =>
        Assert.NotNull(LabourRules.BudgetBlockReason("00010", 100m, null, 0m));

    [Fact]
    public void ApprovalWithinRemainingBudgetPasses() =>
        Assert.Null(LabourRules.BudgetBlockReason("00010", 100m, (1000m, 200m, 300m), 350m)); // remaining 150

    [Fact]
    public void ApprovalBeyondRemainingBudgetBlocks() =>
        Assert.NotNull(LabourRules.BudgetBlockReason("00010", 200m, (1000m, 200m, 300m), 350m)); // remaining 150

    [Fact]
    public void PreviouslyApprovedLabourCountsAgainstBudget() =>
        Assert.NotNull(LabourRules.BudgetBlockReason("00010", 100m, (1000m, 0m, 0m), 950m));

    [Fact]
    public void ExactRemainingBudgetIsAllowed() =>
        Assert.Null(LabourRules.BudgetBlockReason("00010", 150m, (1000m, 200m, 300m), 350m));
}
