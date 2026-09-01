
namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// One navigation destination. Three kinds of Href:
/// - plain routes ("/directory") — navigate and match as-is;
/// - project-scoped templates ("/projects/{project}/financials") — the token is filled with the
///   last-viewed project on navigation, and matches ANY project id when deciding active state;
/// - group keys ("#finance") — never navigated to, they identify accordion groups (and pick
///   the group's icon).
/// MatchPrefixes extend active-state matching to sibling routes an entry spans (templates and a
/// trailing "$" for exact-only are allowed there too). ShallowMatch limits a plain route to
/// itself plus one path segment — "/projects" stays lit on the project list and a project's
/// landing page without stealing every project tab from the grouped entries. ExactMatch makes
/// the HREF itself exact-only (plain or template — a template matches exactly its own tail, so
/// Requests at ".../requests" stays dark on ".../requests/variations", which is Variation
/// Orders' row); MatchPrefixes keep their own prefix semantics and opt into exactness with "$".
/// </summary>
public sealed record NavigationItem(
    string Label,
    string Href,
    IReadOnlyList<string>? MatchPrefixes = null,
    bool ShallowMatch = false,
    bool ExactMatch = false)
{
    public const string ProjectToken = "{project}";

    public bool IsProjectScoped => Href.Contains(ProjectToken, StringComparison.Ordinal);

    /// <summary>The navigable href; project-scoped items fall back to the project list until a
    /// project has been visited.</summary>
    public string ResolveHref(string? projectId) =>
        !IsProjectScoped ? Href
        : string.IsNullOrEmpty(projectId) ? "/projects"
        : Href.Replace(ProjectToken, projectId, StringComparison.Ordinal);

    public bool IsActiveFor(string path) =>
        // ExactMatch governs the Href alone; each prefix keeps prefix semantics unless it opts
        // into exactness itself with a trailing "$".
        Matches(path, Href, ExactMatch) || (MatchPrefixes?.Any(prefix => Matches(path, prefix, exact: false)) ?? false);

    private bool Matches(string path, string href, bool exact)
    {
        // "$" suffix: exact-only for THIS href (plain or template) — used in MatchPrefixes where
        // a plain prefix would swallow a sibling entry's routes.
        if (href.EndsWith('$'))
        {
            exact = true;
            href = href[..^1];
        }

        if (href.Contains(ProjectToken, StringComparison.Ordinal))
        {
            // "/projects/{project}/financials" — any project id in the token's place.
            var parts = href.Split(ProjectToken);
            if (parts.Length != 2) return false;
            if (!path.StartsWith(parts[0], StringComparison.Ordinal)) return false;
            var rest = path[parts[0].Length..];
            // "/projects/{project}" (no suffix) — the project landing page: exactly the id segment.
            if (parts[1].Length == 0) return rest.Length > 0 && !rest.Contains('/');
            var slash = rest.IndexOf('/');
            if (slash < 0) return false;
            var tail = rest[slash..];
            if (exact) return tail == parts[1];
            return tail == parts[1] || tail.StartsWith(parts[1] + "/", StringComparison.Ordinal);
        }

        // Exact-only — used where a prefix would swallow a sibling entry's routes (e.g. Cash
        // Summary at "/finance" must not light up for /finance/xero, which is Xero's).
        if (exact) return path == href;

        if (ShallowMatch)
        {
            if (path == href) return true;
            if (!path.StartsWith(href + "/", StringComparison.Ordinal)) return false;
            return !path[(href.Length + 1)..].Contains('/');
        }

        return path == href || path.StartsWith(href + "/", StringComparison.Ordinal);
    }
}

