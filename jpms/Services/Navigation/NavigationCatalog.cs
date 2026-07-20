using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public static class NavigationCatalog
{
    /// <summary>Every navigable sidebar item for a role, flattened in sidebar order. Every role
    /// uses the same navigation, filtered by RBAC; external roles (architect, client,
    /// subcontractor) are not special-cased onto a separate portal route.</summary>
    public static IReadOnlyList<NavigationItem> ItemsFor(Role role) =>
        DesktopNavigation.ItemsVisibleTo(role);

    public static string HomeRouteFor(Role role)
    {
        // Subcontractors land on their portal home, not the internal dashboard.
        if (role == Role.Subcontractor) return "/portal";
        var items = ItemsFor(role);
        // Home (visible to every role) is always first; ResolveHref(null) keeps a template from
        // ever leaking as a literal href if the catalog changes.
        return items.Count == 0 ? "/dashboard" : items[0].ResolveHref(null);
    }
}
