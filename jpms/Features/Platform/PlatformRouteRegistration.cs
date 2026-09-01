using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Features.Platform;

public static class PlatformRouteRegistration
{
    public static void RegisterPlatformRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetAnnouncedAppVersion, AnnouncedAppVersion>(QueryRoute.Static("/api/system/version"));
        commands.Register<PublishAppVersion, AnnouncedAppVersion>(CommandRoute.Post("/api/system/version/publish"));
    }
}
