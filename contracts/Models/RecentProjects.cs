namespace Jewel.JPMS.Models;

/// <summary>
/// The rule behind the project picker's Recent group: the projects the user last opened, most
/// recent first. This is deliberately a shortcut INTO the canonical list, not a reordering of it —
/// every full project list stays in <see cref="ProjectOrdering.InWorkOrder"/> (see its notes on
/// why stability matters), and recency only ever decides the handful of rows pinned above it.
/// Pure so the rule is testable without a browser; CurrentProjectService (jpms) owns the
/// localStorage plumbing around it.
/// </summary>
public static class RecentProjects
{
    /// <summary>
    /// How many visits are remembered. The picker shows a few besides the current project; the
    /// spares survive a remembered project completing (and so dropping out of the active list)
    /// without the group thinning to nothing.
    /// </summary>
    public const int MaxRemembered = 6;

    /// <summary>
    /// The remembered list after a visit: the visited project moves to (or enters at) the front,
    /// any earlier entry for it goes, the rest keep their order, and the tail is trimmed to
    /// <see cref="MaxRemembered"/>. Ids are compared case-insensitively, matching how project ids
    /// are compared everywhere else.
    /// </summary>
    public static List<string> WithVisit(IReadOnlyList<string> remembered, string projectId)
    {
        var next = new List<string>(MaxRemembered + 1) { projectId };
        next.AddRange(remembered.Where(id =>
            !string.Equals(id, projectId, StringComparison.OrdinalIgnoreCase)));
        if (next.Count > MaxRemembered) next.RemoveRange(MaxRemembered, next.Count - MaxRemembered);
        return next;
    }
}
