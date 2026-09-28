using System.Text.RegularExpressions;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// The words the Contractor's Report may never carry: it goes to the client's side, so nothing in
/// it may read as an admission of defective work. ONE list, read by the report's gate (which
/// refuses a build, naming section and line) and by the phone as the worker types (which offers
/// the defect box, and blocks nothing) — so the two can never disagree.
/// </summary>
public static class ReportWordingRule
{
    public static readonly IReadOnlyList<string> BannedPhrases = new[]
    {
        "remedial works", "remedial", "making good", "made good", "rectification", "rectify", "snagging", "defects", "rework"
    };

    private static readonly Regex Banned = new(
        @"\b(" + string.Join("|", BannedPhrases.Select(Regex.Escape)) + @")\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>The banned words a text contains, lower-cased, each once, in the order found.</summary>
    public static IReadOnlyList<string> BannedWordsIn(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return Banned.Matches(text)
            .Select(match => match.Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
