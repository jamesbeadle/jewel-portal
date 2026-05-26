using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public sealed record NavigationItem(string Label, string Href);

public sealed record DesktopNavigationEntry(NavigationItem Item, IReadOnlyList<Role> VisibleTo)
{
    public bool IsVisibleTo(Role role) => VisibleTo.Contains(role);
}
