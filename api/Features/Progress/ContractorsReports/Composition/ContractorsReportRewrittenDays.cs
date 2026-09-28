using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>
/// Section 1 once the week has been rewritten: a day the rewrite covers prints its house-language
/// bullets as one entry, keeping the day's photographs for Section 9; a day it does not cover
/// prints the raw notes as before. While a flag is open the report cannot be built — the flag
/// list is a gate, not a footnote.
/// </summary>
internal static class ContractorsReportRewrittenDays
{
    private const string RewrittenTitle = "In house language";

    public static IReadOnlyList<ContractorsReportDay> Apply(IReadOnlyList<ContractorsReportDay> days, ContractorsReportRewrite? rewrite)
    {
        if (rewrite is null) return days;
        var rewritten = rewrite.Days.ToDictionary(day => day.Date);
        return days.Select(day => rewritten.TryGetValue(day.Date, out var words) ? Rewritten(day, words) : day).ToList();
    }

    public static IEnumerable<ContractorsReportFinding> OpenFlags(ContractorsReportRewrite? rewrite)
    {
        if (rewrite is null || rewrite.IsCleared) yield break;
        var open = rewrite.OpenFlagCount;
        var line = open == 1 ? "1 flag on the rewrite is still open" : $"{open} flags on the rewrite are still open";
        yield return new ContractorsReportFinding(ContractorsReportSections.Progress, line, "the office clears every flag before the report is built");
    }

    private static ContractorsReportDay Rewritten(ContractorsReportDay day, ContractorsReportRewrittenDay words)
    {
        var entry = new ContractorsReportEntry("", RewrittenTitle, string.Join("\n", words.Bullets), day.Photos);
        return day with { Entries = new[] { entry } };
    }
}
