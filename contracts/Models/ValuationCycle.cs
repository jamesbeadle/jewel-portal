namespace Jewel.JPMS.Models;

public enum ValuationCycle
{
    None = 0,
    Fortnightly = 1,
    FourWeekly = 2,
    Monthly = 3
}

public static class ValuationCycles
{
    public static IReadOnlyList<ValuationCycle> All { get; } = new[]
    {
        ValuationCycle.None, ValuationCycle.Fortnightly, ValuationCycle.FourWeekly, ValuationCycle.Monthly
    };

    public static string DisplayName(this ValuationCycle cycle) => cycle switch
    {
        ValuationCycle.Fortnightly => "Every 2 weeks",
        ValuationCycle.FourWeekly => "Every 4 weeks",
        ValuationCycle.Monthly => "Monthly",
        _ => "Not set"
    };
}
