
namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The human label for whatever page a path lands on. One matcher behind both the header heading
/// (PageHeading) and the assistant panel's context line, so the two can never disagree about what
/// "this page" is called — the panel answers questions about the middle column, and it should name
/// that column exactly as the heading above it does.
/// </summary>
public static class PageContext
{
    /// <summary>The page's label, or null when nothing in the role's catalog matches.</summary>
    public static string? LabelFor(string path, Role? role)
    {
        var trimmed = path.TrimEnd('/');
        if (trimmed is "" or "/dashboard") return "Home";
        if (role is not { } activeRole) return null;
        // Sidebar labels that are deliberately terser than the page deserves: the master to-do
        // list's row reads "Todo" in the Internal folder, but the header spells it out. An item's
        // own page (/todos/{id}) has no sidebar row of its own — label it by what it shows.
        if (trimmed == "/todos") return "To-dos";
        if (trimmed.StartsWith("/todos/", StringComparison.Ordinal)) return "To-do";
        // Catalog items in sidebar order — folder rows put project templates before most company
        // routes, so the more specific project routes win. Template items match any project id,
        // so no project context is needed here.
        foreach (var item in NavigationCatalog.ItemsFor(activeRole))
        {
            if (item.IsActiveFor(trimmed)) return item.Label;
        }
        // The portfolio has no sidebar entry (deliberately) — keep it labelled all the same.
        if (trimmed == "/projects") return "Projects";
        return null;
    }
}
