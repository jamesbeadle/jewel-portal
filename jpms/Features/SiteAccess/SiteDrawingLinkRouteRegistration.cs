using Jewel.JPMS.Contracts.SiteAccess;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.SiteAccess;

internal static class SiteDrawingLinkRouteRegistration
{
    public static void RegisterSiteDrawingLinkRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSiteDrawingLinksForProject, IReadOnlyList<SiteDrawingLink>>(
            new QueryRoute("/api/projects/{projectId}/site-links",
                query => $"/api/projects/{((ListSiteDrawingLinksForProject)query).ProjectId}/site-links"));

        commands.Register<CreateSiteDrawingLink, SiteDrawingLinkCreated>(
            new CommandRoute("POST", "/api/projects/{projectId}/site-links",
                command => $"/api/projects/{((CreateSiteDrawingLink)command).ProjectId}/site-links"));

        commands.Register<RevokeSiteDrawingLink, Acknowledgement>(
            new CommandRoute("POST", "/api/site-links/{siteDrawingLinkId}/revoke",
                command => $"/api/site-links/{((RevokeSiteDrawingLink)command).SiteDrawingLinkId}/revoke"));
    }
}
