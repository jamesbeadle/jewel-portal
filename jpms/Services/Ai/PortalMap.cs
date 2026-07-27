using System.Text;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services.Navigation;

namespace Jewel.JPMS.Services.Ai;

/// <summary>
/// The routes the assistant is allowed to send this user to, as a compact list for the prompt.
///
/// <para><b>Derived, never written.</b> It is a projection of <see cref="DesktopNavigation"/> — the
/// same catalogue the sidebar renders from — so a route that is renamed, moved or role-gated changes
/// here in the same commit. A hand-kept copy would be wrong within a fortnight and the assistant
/// would send people to pages that no longer exist.</para>
///
/// <para>Built per user, so it can only ever describe pages that person can actually reach.</para>
/// </summary>
public static class PortalMap
{
    /// <summary>Cached per role — the catalogue is static, so this is stable for a session.</summary>
    private static readonly Dictionary<Role, string> Cache = new();

    public static string For(Role role)
    {
        if (Cache.TryGetValue(role, out var cached)) return cached;

        var map = new StringBuilder();

        foreach (var folder in DesktopNavigation.FoldersFor(role))
        {
            map.Append("- ").Append(folder.Label).Append(": ");
            map.AppendLine(string.Join(
                "; ",
                folder.Items.Select(item => $"{item.Label} → {item.Href}")));
        }

        // The portfolio has no sidebar row on purpose, but it is a real and useful destination.
        if (DesktopNavigation.CanSeeProjects(role))
            map.AppendLine("- Portfolio: All projects → /projects");

        var built = map.ToString().TrimEnd();
        Cache[role] = built;
        return built;
    }
}
