using Jewel.JPMS.Contracts.Bluebeam;

namespace Jewel.JPMS.Features.Bluebeam;

public static class BluebeamRouteRegistration
{
    public static void RegisterBluebeamRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetBluebeamStatus, BluebeamStatus>(
            QueryRoute.Static("/api/bluebeam/status"));

        commands.Register<StartBluebeamConnect, BluebeamConnectStart>(
            CommandRoute.Post("/api/bluebeam/connect/start"));

        commands.Register<DisconnectBluebeam, BluebeamStatus>(
            CommandRoute.Post("/api/bluebeam/disconnect"));
    }
}
