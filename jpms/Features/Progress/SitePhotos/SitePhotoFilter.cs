using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

/// <summary>The pool's views. Unfiled is the working view — what is still waiting for the
/// weekly-report run — so it is the default; Archived is what the run set aside as not for the
/// report, kept just in case. With a project in scope (the project selected in the menu, the
/// FD's ask of 2026-09-24) Filed and Archived narrow to that project's photos; Unfiled stays the
/// whole pool's, because an unfiled photo has no project until the run files it.</summary>
public static class SitePhotoFilter
{
    public const string Unfiled = "unfiled";
    public const string Filed = "filed";
    public const string Archived = "archived";
    public const string All = "all";

    public static IReadOnlyList<TabItem> Chips(IReadOnlyList<SitePhoto>? photos, string? projectId)
    {
        var inScope = photos is null ? null : InScope(photos, projectId);
        return new[]
        {
            new TabItem(Unfiled, "Unfiled", Count: inScope?.Count(photo => photo.IsWaiting)),
            new TabItem(Filed, "Filed", Count: inScope?.Count(photo => photo.IsFiled)),
            new TabItem(Archived, "Archived", Count: inScope?.Count(photo => photo.IsArchived)),
            new TabItem(All, "All", Count: inScope?.Count)
        };
    }

    public static IReadOnlyList<SitePhoto> Apply(IReadOnlyList<SitePhoto> photos, string filter, string? projectId)
    {
        var inScope = InScope(photos, projectId);
        return filter switch
        {
            Unfiled => inScope.Where(photo => photo.IsWaiting).ToList(),
            Filed => inScope.Where(photo => photo.IsFiled).ToList(),
            Archived => inScope.Where(photo => photo.IsArchived).ToList(),
            _ => inScope
        };
    }

    private static IReadOnlyList<SitePhoto> InScope(IReadOnlyList<SitePhoto> photos, string? projectId)
    {
        if (projectId is null) return photos;
        return photos.Where(photo => photo.IsWaiting || photo.BelongsTo(projectId)).ToList();
    }

    public static string EmptyMessage(string filter, bool canContribute, string? projectReference) => filter switch
    {
        Unfiled => canContribute
            ? "Nothing waiting — drop the week's photographs above and they will be filed by the weekly-report run."
            : "Nothing waiting to be filed.",
        Filed => projectReference is null
            ? "No photographs have been filed onto a progress update yet."
            : $"No photographs have been filed onto {projectReference}'s progress updates yet.",
        Archived => "Nothing archived — the weekly-report run archives the photographs it leaves out of the report.",
        _ => canContribute ? "No photographs in the pool yet — drop the week's above." : "No photographs in the pool yet."
    };
}
