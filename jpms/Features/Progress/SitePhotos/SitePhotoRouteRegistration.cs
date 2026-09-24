using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

internal static class SitePhotoRouteRegistration
{
    // The pool's upload is multipart/form-data, sent by HttpSitePhotoStore; the fingerprint match,
    // the filing and the archiving are the connector's, so only the list, the restore and the
    // delete are routed here.
    public static void RegisterSitePhotoRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSitePhotos, IReadOnlyList<SitePhoto>>(
            new QueryRoute("/api/site-photos",
                query => ((ListSitePhotos)query).UnfiledOnly ? "/api/site-photos?unfiled=1" : "/api/site-photos"));

        commands.Register<DeleteSitePhoto, Acknowledgement>(
            new CommandRoute("DELETE", "/api/site-photos/{sitePhotoId}",
                command => $"/api/site-photos/{((DeleteSitePhoto)command).SitePhotoId}"));

        commands.Register<RestoreSitePhoto, Acknowledgement>(
            new CommandRoute("POST", "/api/site-photos/{sitePhotoId}/restore",
                command => $"/api/site-photos/{((RestoreSitePhoto)command).SitePhotoId}/restore"));
    }
}
