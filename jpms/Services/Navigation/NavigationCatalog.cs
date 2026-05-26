using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public static class NavigationCatalog
{
    public static IReadOnlyList<NavigationItem> ItemsFor(Role role)
    {
        if (role.IsExternal()) return PortalNavigation.ItemsFor(role);
        return DesktopNavigation.ItemsVisibleTo(role);
    }

    public static string HomeRouteFor(Role role)
    {
        var items = ItemsFor(role);
        return items.Count == 0 ? "/dashboard" : items[0].Href;
    }
}
