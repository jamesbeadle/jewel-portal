using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.CommercialInputs;

public static class CommercialInputsRouteRegistration
{
    public static void RegisterCommercialInputsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListDayworksForProject, IReadOnlyList<Daywork>>(
            new QueryRoute("/api/projects/{projectId}/dayworks",
                query => $"/api/projects/{((ListDayworksForProject)query).ProjectId}/dayworks"));

        commands.Register<LogDaywork, Daywork>(
            new CommandRoute("POST", "/api/projects/{projectId}/dayworks",
                command => $"/api/projects/{((LogDaywork)command).ProjectId}/dayworks"));
    }
}
