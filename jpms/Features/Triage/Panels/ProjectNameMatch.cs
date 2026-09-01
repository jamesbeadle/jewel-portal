
namespace Jewel.JPMS.Features.Triage.Panels;

/// <summary>
/// Whether an email reads as belonging to a project, judged the way the Control Centre's project
/// pre-fill judges it: the project's name appears verbatim (case-insensitive) in the subject or
/// opening line. Record-less tags such as the Subcontractor-communication family carry no project,
/// so this name match is the only way the communications browser can narrow to "this project"
/// (Nigel, 2026-08-22 — the all-projects feed "could get quite hefty").
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
        var name = projectName.Trim();
        return email.Subject.Contains(name, StringComparison.OrdinalIgnoreCase)
            || email.BodyPreview.Contains(name, StringComparison.OrdinalIgnoreCase);
    }
}
