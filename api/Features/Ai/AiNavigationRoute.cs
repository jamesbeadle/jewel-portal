namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// The pure half of navigate_to's server-side check: reading the project out of a portal route,
/// spotting a site-map placeholder the model forgot to substitute, and rewriting the route with
/// the project's real id once the runner has resolved it.
///
/// <para>Why this exists (live failure 2026-08-25): the site map hands the model
/// <c>/projects/{project}/requests/rfis</c> with "{project} means the project in view", and a
/// user standing on Abbot Road said "load By France RFIs". The model made one call —
/// navigate_to — with no By France id to put in the route, the server answered <c>ok: true</c>
/// (a Ui tool's ok meant only "posted"), the browser landed on "Project not found" and the model
/// narrated the By France register it never reached. Everything here is pure so it can be
/// pinned by tests; the database look-up lives in <see cref="AiTurnRunner"/>.</para>
/// </summary>
internal static class AiNavigationRoute
{
    private const string ProjectsPrefix = "/projects/";

    /// <summary>One project as the matcher sees it — the three things a model might put in a
    /// route: the real id, the reference (JBB-2026-001) or the name (By France).</summary>
    public sealed record Candidate(string ProjectId, string Reference, string Name);

    /// <summary>What matching a route's project segment against the projects found.</summary>
    public sealed record Match(Candidate? Project, IReadOnlyList<Candidate> Ambiguous)
    {
        public bool Found => Project is not null;
        public bool IsAmbiguous => Ambiguous.Count > 1;
    }

    /// <summary>A portal path: starts with a single slash and carries no backslash (browsers read
    /// "/\host/…" as "//host/…"), so it can never become an off-site redirect. Mirrors the client's
    /// own guard in ChatPanel.Navigate.</summary>
    public static bool IsPortalPath(string? route) =>
        !string.IsNullOrWhiteSpace(route)
        && route.StartsWith('/')
        && !route.StartsWith("//", StringComparison.Ordinal)
        && !route.Contains('\\');

    /// <summary>
    /// The segment naming the project — whatever sits between "/projects/" and the next slash,
    /// query or fragment — or null when the route is not under a project (the portfolio at
    /// /projects, the Control Centre, /rfis). Returned decoded, so a model that URL-encoded its
    /// braces ("%7Bproject%7D") is still read as the placeholder it is.
    /// </summary>
    public static string? ProjectSegment(string route)
    {
        var (start, length) = ProjectSegmentSpan(route);
        if (start < 0 || length == 0) return null;
        return Uri.UnescapeDataString(route.Substring(start, length));
    }

    /// <summary>A site-map template segment the model sent verbatim: "{project}", "{id}",
    /// "{projectId}" — any braces at all.</summary>
    public static bool IsPlaceholder(string? segment) =>
        !string.IsNullOrWhiteSpace(segment)
        && segment.StartsWith('{')
        && segment.EndsWith('}');

    /// <summary>The route with its project segment replaced by <paramref name="projectId"/>; the
    /// rest of the path, the query and the fragment are kept as they were. A route with no
    /// project segment comes back unchanged.</summary>
    public static string WithProject(string route, string projectId)
    {
        var (start, length) = ProjectSegmentSpan(route);
        if (start < 0 || length == 0) return route;
        return route.Substring(0, start) + projectId + route.Substring(start + length);
    }

    /// <summary>
    /// The first "{…}" left anywhere in the route's path (decoded), or null when there is none.
    /// After the project segment has been substituted this is what catches
    /// "/projects/3490…/variations/{id}" — a record template sent instead of the ready-made route
    /// a tool would have returned.
    /// </summary>
    public static string? FirstPlaceholder(string route)
    {
        var path = Uri.UnescapeDataString(PathOnly(route));
        var open = path.IndexOf('{');
        if (open < 0) return null;
        var close = path.IndexOf('}', open + 1);
        return close < 0 ? path.Substring(open) : path.Substring(open, close - open + 1);
    }

    /// <summary>
    /// Which project the segment means. The real id wins outright; failing that the reference
    /// (case-insensitive), then the exact name, then a name that merely contains the words — but
    /// a contains-match must be UNIQUE, since "Beresford" naming two phases is exactly the case
    /// where guessing sends someone to the wrong job. Whitespace and case are forgiven throughout.
    /// </summary>
    public static Match Resolve(string segment, IReadOnlyList<Candidate> projects)
    {
        var wanted = segment.Trim();
        if (wanted.Length == 0) return new Match(null, Array.Empty<Candidate>());

        var byId = projects.FirstOrDefault(row => Same(row.ProjectId, wanted));
        if (byId is not null) return new Match(byId, Array.Empty<Candidate>());

        var byReference = projects.FirstOrDefault(row => Same(row.Reference, wanted));
        if (byReference is not null) return new Match(byReference, Array.Empty<Candidate>());

        var byName = projects.Where(row => Same(row.Name, wanted)).ToList();
        if (byName.Count == 1) return new Match(byName[0], Array.Empty<Candidate>());
        if (byName.Count > 1) return new Match(null, byName);

        var containing = projects
            .Where(row => row.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (containing.Count == 1) return new Match(containing[0], Array.Empty<Candidate>());
        return new Match(null, containing);
    }

    private static bool Same(string a, string b) =>
        string.Equals(a.Trim(), b, StringComparison.OrdinalIgnoreCase);

    private static string PathOnly(string route)
    {
        var end = route.IndexOfAny(new[] { '?', '#' });
        return end < 0 ? route : route.Substring(0, end);
    }

    /// <summary>(start, length) of the project segment inside the raw route, or (-1, 0).</summary>
    private static (int Start, int Length) ProjectSegmentSpan(string route)
    {
        if (!route.StartsWith(ProjectsPrefix, StringComparison.OrdinalIgnoreCase)) return (-1, 0);
        var start = ProjectsPrefix.Length;
        var end = route.IndexOfAny(new[] { '/', '?', '#' }, start);
        var length = (end < 0 ? route.Length : end) - start;
        return length == 0 ? (-1, 0) : (start, length);
    }
}
