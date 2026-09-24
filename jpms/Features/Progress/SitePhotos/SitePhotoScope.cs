namespace Jewel.JPMS.Features.Progress.SitePhotos;

/// <summary>Whose photographs the pool shows: the project selected in the menu, or the whole pool.
/// The page opens on the project (2026-09-24, the FD: Site Photos sat under By France and read as
/// By France's photos when it was every site's).</summary>
public static class SitePhotoScope
{
    public const string WholePool = "pool";

    public static IReadOnlyList<TabItem> Chips(Project? project)
    {
        var wholePool = new TabItem(WholePool, "Whole pool", Title: "Every site's photographs");
        if (project is null) return new[] { wholePool };
        return new[] { new TabItem(project.ProjectId, project.Reference, Title: project.Name), wholePool };
    }

    public static string? ProjectIdOf(string scope) => scope == WholePool ? null : scope;
}
