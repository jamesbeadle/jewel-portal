namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>
/// Where the pool's files live in the progress photo store. The store keys every file
/// <c>{project}/{update}/{photo}/{name}</c>; a pool photo belongs to no project and no update
/// yet, so it sits under these two fixed keys — <c>site-photos/pool/{sitePhotoId}/{name}</c> —
/// and moves to its update's own key when it is filed.
/// </summary>
internal static class SitePhotoPool
{
    public const string ProjectKey = "site-photos";
    public const string UpdateKey = "pool";
}
