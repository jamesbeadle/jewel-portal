
namespace Jewel.JPMS.Services.Navigation;

public static class NavigationCatalog
{
    /// <summary>Every navigable sidebar item for a role, flattened in sidebar order. Every role
    /// uses the same navigation, filtered by RBAC; the client and the architect open the same
    /// views as everyone else, tailored to the role.</summary>
    public static IReadOnlyList<NavigationItem> ItemsFor(Role role) =>
        DesktopNavigation.ItemsVisibleTo(role);

    public static string HomeRouteFor(Role role)
    {
        // Subcontractor accounts land on their own home, not the dashboard.
        if (role == Role.Subcontractor) return "/portal";
        var items = ItemsFor(role);
        // Home (visible to every role) is always first; ResolveHref(null) keeps a template from
        // ever leaking as a literal href if the catalog changes.
        return items.Count == 0 ? "/dashboard" : items[0].ResolveHref(null);
    }
}
