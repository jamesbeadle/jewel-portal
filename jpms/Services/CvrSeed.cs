using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

internal static class CvrSeed
{
    public static List<CvrSnapshot> Snapshots(DateTimeOffset baseDate) => new()
    {
        new("CVR-001", "PRJ-001", baseDate.AddDays(56),  2_400_000m, 1_980_000m, 2_400_000m, 420_000m, 17.5m,  0),
        new("CVR-002", "PRJ-001", baseDate.AddDays(28),  2_400_000m, 1_995_000m, 2_400_000m, 405_000m, 16.9m, -1)
    };

    public static List<CvrPackageRow> PackageRows() => new()
    {
        new("PRJ-001", "Groundworks",      180_000m, 220_000m,   6_500m,  8_200m, +3_400m),
        new("PRJ-001", "Brickwork",        160_000m, 200_000m,        0,       0, +1_200m),
        new("PRJ-001", "Roofing",          240_000m, 300_000m,  12_000m, 15_000m, -2_100m),
        new("PRJ-001", "Electrical",       110_000m, 140_000m,        0,       0,      0m),
        new("PRJ-001", "Joinery",           95_000m, 130_000m,   6_500m,  9_400m, +5_000m),
        new("PRJ-001", "Plumbing",          85_000m, 110_000m,        0,       0,      0m)
    };

    public static List<ForecastComponent> ForecastComponents() => new()
    {
        new("FC-001", "PRJ-001", "Groundworks",  82_400m,  85_000m,   2_000m,        0, 10_600m),
        new("FC-002", "PRJ-001", "Brickwork",    38_900m,  88_000m,   1_000m,        0, 32_100m),
        new("FC-003", "PRJ-001", "Roofing",      47_500m, 120_000m,   3_000m,        0, 81_500m),
        new("FC-004", "PRJ-001", "Electrical",    9_200m,  85_000m,   1_500m,        0, 14_300m),
        new("FC-005", "PRJ-001", "Joinery",      12_400m,  60_000m,   2_000m,        0, 26_000m),
        new("FC-006", "PRJ-001", "Plumbing",      8_400m,  72_000m,   1_000m,        0,  3_600m)
    };

    public static List<QsAccrual> Accruals(string ownerEmail) => new()
    {
        new("QA-001", "PRJ-001", "Groundworks", "Anticipated extra dig depth on north edge", 2_000m, 0m, 0m, ownerEmail, DateTimeOffset.UtcNow.AddDays(-14)),
        new("QA-002", "PRJ-001", "Roofing",     "Slate batten upgrade — pending sign-off",   3_000m, 0m, 0m, ownerEmail, DateTimeOffset.UtcNow.AddDays(-7)),
        new("QA-003", "PRJ-001", "Brickwork",   "Brick samples returned — saving expected",       0m, 1_500m, 0m, ownerEmail, DateTimeOffset.UtcNow.AddDays(-5))
    };

    public static List<PrelimItem> PrelimItems() => new()
    {
        new("PR-001", "PRJ-001", "Project Manager"),
        new("PR-002", "PRJ-001", "Site Manager"),
        new("PR-003", "PRJ-001", "Welfare"),
        new("PR-004", "PRJ-001", "Hoarding"),
        new("PR-005", "PRJ-001", "Plant hire"),
        new("PR-006", "PRJ-001", "Labour hire")
    };

    public static List<PrelimForecastEntry> PrelimEntries(List<PrelimItem> items)
    {
        var entries = new List<PrelimForecastEntry>();
        var weeks = new[] { 1, 2, 3, 4, 5, 6 };
        var rates = new Dictionary<string, decimal> { ["PR-001"] = 1_400m, ["PR-002"] = 1_200m, ["PR-003"] = 320m, ["PR-004"] = 180m, ["PR-005"] = 650m, ["PR-006"] = 900m };
        foreach (var item in items)
        {
            var rate = rates[item.PrelimItemId];
            foreach (var week in weeks)
            {
                entries.Add(new PrelimForecastEntry(
                    $"PFE-{item.PrelimItemId}-{week}", item.PrelimItemId, week,
                    rate, rate * 1.02m, rate * 1.03m));
            }
        }
        return entries;
    }

    public static List<Eot> Eots() => new()
    {
        new("EOT-001", "PRJ-001", "Architect drawing revision delay", 7, 4_500m, DateTimeOffset.UtcNow.AddDays(-30))
    };
}
