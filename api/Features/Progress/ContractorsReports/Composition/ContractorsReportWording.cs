using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>
/// The wording gate the Contractor's Report is issued under: it goes to the client's side, so
/// nothing in it may read as an admission of defective work. Every printable line is checked
/// against the one shared list (<see cref="ReportWordingRule"/> — the phone reads the same list
/// as the worker types) and a hit is a finding naming the section and the line: the build is
/// refused, never silently reworded.
/// </summary>
internal static class ContractorsReportWording
{
    public static IReadOnlyList<ContractorsReportFinding> Check(IEnumerable<(string Section, string Line)> lines) =>
        lines
            .Where(line => !string.IsNullOrWhiteSpace(line.Line))
            .SelectMany(line => FindingsIn(line.Section, line.Line))
            .ToList();

    private static IEnumerable<ContractorsReportFinding> FindingsIn(string section, string line) =>
        ReportWordingRule.BannedWordsIn(line)
            .Select(word => new ContractorsReportFinding(section, Excerpt(line), $"contains \"{word}\""));

    private const int ExcerptLength = 120;

    private static string Excerpt(string line)
    {
        var flat = line.ReplaceLineEndings(" ").Trim();
        return flat.Length <= ExcerptLength ? flat : flat[..ExcerptLength] + "…";
    }
}
