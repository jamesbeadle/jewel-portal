using System.Text;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services.Navigation;

namespace Jewel.JPMS.Services.Ai;

/// <summary>
/// The assistant's map of the ENTIRE portal this user can reach: every sidebar destination, the
/// standalone work queues at the foot of the rail (the Control Centre above all), and the record
/// detail pages — each with a one-line note of what it is and what can be done there. This is what
/// lets the orchestrator hold the whole site in mind when someone says "go to…", "open…", "draft…".
///
/// <para><b>Derived, never written.</b> The route list is a projection of
/// <see cref="DesktopNavigation"/> — the same catalogue the sidebar renders from — so a route that
/// is renamed, moved or role-gated changes here in the same commit. The capability notes live in
/// <see cref="PortalMapCapabilities"/> and only ever ANNOTATE a derived route: a note whose route
/// left the catalogue stops rendering. A hand-kept copy of the whole thing would be wrong within a
/// fortnight and the assistant would send people to pages that no longer exist.</para>
///
/// <para>Built per role, so it can only ever describe pages that person can actually reach. Cached
/// per role — stable for a session, which also keeps the system prompt byte-stable for caching.</para>
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
            map.Append("- ").Append(folder.Label).AppendLine(":");
            foreach (var item in folder.Items)
                map.AppendLine(Line(item));
        }

        // The folderless rows at the foot of the rail — the standing work queues (the Control
        // Centre, Document Control, Xero Cost Allocation) and the live Valuation Report. They are
        // real destinations exactly like the folder rows; leaving them out once left the assistant
        // not knowing the Control Centre existed.
        var standalone = DesktopNavigation.StandaloneItemsFor(role);
        if (standalone.Count > 0)
        {
            map.AppendLine("- Work queues and standing pages:");
            foreach (var item in standalone)
                map.AppendLine(Line(item));
        }

        // The portfolio has no sidebar row on purpose, but it is a real and useful destination.
        if (DesktopNavigation.CanSeeProjects(role))
        {
            map.AppendLine("- Portfolio:");
            map.AppendLine("  All projects → /projects — every project with reference, name and stage");
        }

        // Record detail pages have no sidebar row to derive from — they are reached from their
        // register, or from the ready-made `route` most read tools return (prefer those routes).
        map.AppendLine("- Record detail pages (substitute real ids — tools return ready-made routes):");
        foreach (var page in PortalMapCapabilities.DetailPages)
            map.Append("  ").AppendLine(page);

        var built = map.ToString().TrimEnd();
        Cache[role] = built;
        return built;
    }

    /// <summary>One row: "  Label → href — what it is and what can be done there".</summary>
    private static string Line(NavigationItem item)
    {
        var note = PortalMapCapabilities.For(item.Href);
        return note is null
            ? $"  {item.Label} → {item.Href}"
            : $"  {item.Label} → {item.Href} — {note}";
    }
}
