using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public static class NavigationCatalog
{
    /// <summary>The nav tree for a role — plain entries and accordion groups. Every role uses the
    /// same navigation, filtered by RBAC; external roles (architect, client, subcontractor) are
    /// not special-cased onto a separate portal route.</summary>
    public static IReadOnlyList<NavigationNode> NodesFor(Role role) =>
        DesktopNavigation.NodesVisibleTo(role);

    /// <summary>The tree flattened to navigable items (group children in place of the group) —
    /// for consumers that don't render the accordion, like the page-heading matcher.</summary>
    public static IReadOnlyList<NavigationItem> ItemsFor(Role role) =>
        NodesFor(role)
            .SelectMany(node => node.IsGroup ? node.Children : (IEnumerable<NavigationItem>)new[] { node.Item })
            .ToList();

    public static string HomeRouteFor(Role role)
    {
        // Subcontractors land on their portal home, not the internal dashboard.
        if (role == Role.Subcontractor) return "/portal";
        var items = ItemsFor(role);
        // Dashboard (visible to every role) is always first; ResolveHref(null) keeps a template
        // from ever leaking as a literal href if the catalog changes.
        return items.Count == 0 ? "/dashboard" : items[0].ResolveHref(null);
    }
}
