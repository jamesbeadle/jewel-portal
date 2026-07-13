using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>
/// Pure labour-tracking rules. They live in contracts (like XeroSplitMaths) so the test
/// project can exercise them without referencing the API host.
/// </summary>
public static class LabourRules
{
    /// <summary>Hours must be at least half an hour, in half-hour steps (spec constraint).</summary>
    public static bool IsValidHours(decimal hours) =>
        hours >= 0.5m && hours % 0.5m == 0m;

    /// <summary>Validates an end-of-day allocation. Returns error messages; empty = valid.</summary>
    public static IReadOnlyList<string> CheckSignOutEntries(
        IReadOnlyList<SiteSignOutEntry> entries, IReadOnlySet<string> allowedCostCodes)
    {
        var errors = new List<string>();
        if (entries.Count == 0) { errors.Add("At least one task with hours is required."); return errors; }
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.CostCode))
                errors.Add("Every entry needs a cost code.");
            else if (!allowedCostCodes.Contains(entry.CostCode))
                errors.Add($"Cost code {entry.CostCode} is not on this project's list.");
            if (!IsValidHours(entry.Hours))
                errors.Add($"Hours must be in half-hour steps of at least 0.5 (got {entry.Hours}).");
        }
        if (entries.GroupBy(entry => entry.CostCode).Any(group => group.Count() > 1))
            errors.Add("Each cost code can only appear once per day.");
        return errors;
    }

    /// <summary>
    /// The rate effective on the worked date: the latest history row with
    /// EffectiveFrom ≤ workedOn, falling back to the worker's current rate for dates before any
    /// history exists. Snapshotted onto the timesheet at approval.
    /// </summary>
    public static decimal ResolveRate(
        IReadOnlyList<(DateTimeOffset EffectiveFrom, decimal HourlyRate)> historyOldestFirst,
        decimal currentRate, DateTimeOffset workedOn)
    {
        var effective = currentRate;
        foreach (var (from, rate) in historyOldestFirst)
            if (from <= workedOn) effective = rate;
        return effective;
    }

    public static decimal CostOf(decimal hours, decimal rate) => decimal.Round(hours * rate, 2);

    /// <summary>
    /// The budget hard-block (workflow 07-D): approving labour cost against a cost code must not
    /// exceed the remaining budget (allocated − spent − committed). No budget row at all is
    /// treated as no budget — blocked. Returns null when allowed, otherwise the reason.
    /// </summary>
    public static string? BudgetBlockReason(
        string costCode, decimal newCost,
        (decimal Allocated, decimal Spent, decimal Committed)? budget,
        decimal alreadyApprovedLabour)
    {
        if (budget is null)
            return $"No budget is set for cost code {costCode} — raise a work order or allocate budget first.";
        var remaining = budget.Value.Allocated - budget.Value.Spent - budget.Value.Committed - alreadyApprovedLabour;
        if (newCost > remaining)
            return $"Approving £{newCost:N2} against {costCode} exceeds its remaining budget of £{remaining:N2} — raise a work order or re-allocate budget.";
        return null;
    }
}
