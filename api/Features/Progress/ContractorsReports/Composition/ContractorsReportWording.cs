using System.Text.RegularExpressions;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>
/// The wording rule the Contractor's Report is issued under: it goes to the client's side, so
/// nothing in it may read as an admission of defective work. Every printable line is checked
/// and a hit is a finding naming the section and the line — the build is refused, never
/// silently reworded.
/// </summary>
internal static class ContractorsReportWording
{
    public static readonly IReadOnlyList<string> BannedPhrases = new[]
    {
        "remedial works", "remedial", "making good", "rectification", "rectify", "snagging", "defects", "rework"
    };

    private static readonly Regex Banned = new(
        @"\b(" + string.Join("|", BannedPhrases.Select(Regex.Escape)) + @")\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<ContractorsReportFinding> Check(IEnumerable<(string Section, string Line)> lines) =>
        lines
            .Where(line => !string.IsNullOrWhiteSpace(line.Line))
            .SelectMany(line => FindingsIn(line.Section, line.Line))
            .ToList();

    private static IEnumerable<ContractorsReportFinding> FindingsIn(string section, string line) =>
        Banned.Matches(line)
            .Select(match => match.Value.ToLowerInvariant())
            .Distinct()
            .Select(word => new ContractorsReportFinding(section, Excerpt(line), $"contains \"{word}\""));

    private const int ExcerptLength = 120;

    private static string Excerpt(string line)
    {
        var flat = line.ReplaceLineEndings(" ").Trim();
        return flat.Length <= ExcerptLength ? flat : flat[..ExcerptLength] + "…";
    }
}
