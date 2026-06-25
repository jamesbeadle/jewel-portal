using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public static class NavigationCatalog
{
    public static IReadOnlyList<NavigationItem> ItemsFor(Role role)
    {
        // Every role uses the same navigation, filtered by RBAC. External roles
        // (architect, client, subcontractor) are not special-cased onto a separate
        // portal route — what they see is governed by per-entry role visibility.
        return DesktopNavigation.ItemsVisibleTo(role);
    }

    public static string HomeRouteFor(Role role)
    {
        var items = ItemsFor(role);
        return items.Count == 0 ? "/dashboard" : items[0].Href;
    }
}
