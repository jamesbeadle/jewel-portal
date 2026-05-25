using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

internal static class CommercialSeed
{
    public static List<ClaimPeriod> ClaimPeriods(DateTimeOffset baseDate) => new()
    {
        new("CP-001", "PRJ-001", 1, baseDate.AddDays(0),   baseDate.AddDays(28)),
        new("CP-002", "PRJ-001", 2, baseDate.AddDays(28),  baseDate.AddDays(56)),
        new("CP-003", "PRJ-001", 3, baseDate.AddDays(56),  baseDate.AddDays(84))
    };

    public static List<Valuation> Valuations(List<ClaimPeriod> claimPeriods) => new()
    {
        new("VL-001", claimPeriods[0].ClaimPeriodId, "PRJ-001",  220_000m, 3m, 213_400m, true,  claimPeriods[0].EndDate),
        new("VL-002", claimPeriods[1].ClaimPeriodId, "PRJ-001",  340_000m, 3m, 329_800m, true,  claimPeriods[1].EndDate),
        new("VL-003", claimPeriods[2].ClaimPeriodId, "PRJ-001",  380_000m, 3m, 368_600m, false, null)
    };

    public static List<CostCodeBudget> Budgets() => new()
    {
        new("CB-001", "PRJ-001", "GW-100",   85_000m,  82_400m),
        new("CB-002", "PRJ-001", "GW-110",   42_000m,  38_900m),
        new("CB-003", "PRJ-001", "EX-200",   65_000m,  58_300m),
        new("CB-004", "PRJ-001", "RF-300",   95_000m,  47_500m),
        new("CB-005", "PRJ-001", "EL-400",   31_000m,   9_200m),
        new("CB-006", "PRJ-001", "JN-500",   28_000m,  12_400m)
    };

    public static List<Timesheet> Timesheets(string ownerEmail) => new()
    {
        new("TS-001", "PRJ-001", ownerEmail, DateTimeOffset.UtcNow.AddDays(-1), 6m,  "GW-110", true),
        new("TS-002", "PRJ-001", ownerEmail, DateTimeOffset.UtcNow.AddDays(-2), 8m,  "EX-200", true),
        new("TS-003", "PRJ-001", ownerEmail, DateTimeOffset.UtcNow.AddDays(-3), 5m,  "RF-300", false),
        new("TS-004", "PRJ-001", ownerEmail, DateTimeOffset.UtcNow.AddDays(-4), 7m,  "JN-500", false),
        new("TS-005", "PRJ-001", ownerEmail, DateTimeOffset.UtcNow.AddDays(-5), 4m,  "EL-400", false)
    };

    public static CashflowSnapshot LatestCashflow() => new(
        "CF-001", DateTimeOffset.UtcNow,
        1_180_000m, 920_000m, 260_000m);
}
