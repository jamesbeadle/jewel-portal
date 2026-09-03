using System.Text.RegularExpressions;

namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// Whether an email reads as belonging to a project, judged the way the Control Centre's project
/// pre-fill judges it: the project's name appears as a whole word (case-insensitive) in the subject
/// or opening line. Record-less tags such as the Subcontractor-communication family carry no project,
/// so this name match is the only way the communications browser can narrow to "this project"
/// (Nigel, 2026-08-22 — the all-projects feed "could get quite hefty").
///
/// Whole words, not substrings: a lead project named "Test" used to match every thread that said
/// "latest", which made the Control Centre's pre-fill see two projects on an Abbot Road thread and
/// silently give up (2026-08-28). A name now has to stand on its own — letters or digits on either
/// side of it break the match.
/// </summary>
public static class ProjectNameMatch
{
    /// <summary>Names shorter than this are too common to be an honest match.</summary>
    private const int MinimumNameLength = 4;

    public static bool IsUsable(string? projectName) =>
        (projectName ?? "").Trim().Length >= MinimumNameLength;

    public static bool Mentions(MailboxMessage email, string projectName)
    {
        if (!IsUsable(projectName)) return true;
        return MentionsName(email.Subject, projectName) || MentionsName(email.BodyPreview, projectName);
    }

    /// <summary>
    /// Whether <paramref name="text"/> contains <paramref name="projectName"/> as a whole word or
    /// phrase: case-insensitive, whitespace inside the name matched loosely (one or more spaces,
    /// tabs or line breaks), and no letter or digit immediately before or after it. An unusable
    /// (too short) name never matches — the caller decides what "no opinion" means.
    /// </summary>
    public static bool MentionsName(string? text, string? projectName)
    {
        if (string.IsNullOrEmpty(text) || !IsUsable(projectName)) return false;
        return PatternFor(projectName!.Trim()).IsMatch(text);
    }

    // One compiled pattern per project name — the Control Centre runs every live project's name
    // over every opened email, so the handful of names in play are cached rather than rebuilt.
    private static readonly Dictionary<string, Regex> patterns = new(StringComparer.Ordinal);

    private static Regex PatternFor(string name)
    {
        lock (patterns)
        {
            if (patterns.TryGetValue(name, out var cached)) return cached;
            var words = Regex.Split(name, @"\s+").Where(word => word.Length > 0).Select(Regex.Escape);
            var pattern = @"(?<![\p{L}\p{N}])" + string.Join(@"\s+", words) + @"(?![\p{L}\p{N}])";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            patterns[name] = regex;
            return regex;
        }
    }
}
