using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public sealed record NavigationItem(string Label, string Href, IReadOnlyList<string>? MatchPrefixes = null)
{
    /// <summary>
    /// Whether this entry should render as active for the given path. Grouped entries (e.g.
    /// Financials) stay lit across every page folded into them via <see cref="MatchPrefixes"/>.
    /// </summary>
    public bool IsActiveFor(string path) =>
        Matches(path, Href) || (MatchPrefixes?.Any(prefix => Matches(path, prefix)) ?? false);

    private static bool Matches(string path, string href) =>
        path == href || path.StartsWith(href + "/", StringComparison.Ordinal);
}

public sealed record DesktopNavigationEntry(NavigationItem Item, IReadOnlyList<Role> VisibleTo)
{
    public bool IsVisibleTo(Role role) => VisibleTo.Contains(role);
}
