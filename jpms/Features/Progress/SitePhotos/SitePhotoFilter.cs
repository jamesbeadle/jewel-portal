using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

/// <summary>The pool's views. Unfiled is the working view — what is still waiting for the
/// weekly-report run — so it is the default; Archived is what the run set aside as not for the
/// report, kept just in case.</summary>
public static class SitePhotoFilter
{
    public const string Unfiled = "unfiled";
    public const string Filed = "filed";
    public const string Archived = "archived";
    public const string All = "all";

    public static IReadOnlyList<TabItem> Chips(IReadOnlyList<SitePhoto>? photos) => new[]
    {
        new TabItem(Unfiled, "Unfiled", Count: photos?.Count(photo => photo.IsWaiting)),
        new TabItem(Filed, "Filed", Count: photos?.Count(photo => photo.IsFiled)),
        new TabItem(Archived, "Archived", Count: photos?.Count(photo => photo.IsArchived)),
        new TabItem(All, "All", Count: photos?.Count)
    };

    public static IReadOnlyList<SitePhoto> Apply(IReadOnlyList<SitePhoto> photos, string filter) => filter switch
    {
        Unfiled => photos.Where(photo => photo.IsWaiting).ToList(),
        Filed => photos.Where(photo => photo.IsFiled).ToList(),
        Archived => photos.Where(photo => photo.IsArchived).ToList(),
        _ => photos
    };

    public static string EmptyMessage(string filter, bool canContribute) => filter switch
    {
        Unfiled => canContribute
            ? "Nothing waiting — drop the week's photographs above and they will be filed by the weekly-report run."
            : "Nothing waiting to be filed.",
        Filed => "No photographs have been filed onto a progress update yet.",
        Archived => "Nothing archived — the weekly-report run archives the photographs it leaves out of the report.",
        _ => canContribute ? "No photographs in the pool yet — drop the week's above." : "No photographs in the pool yet."
    };
}
