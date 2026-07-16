using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Lads;

// Client routes for Liquidated Damages claims. Mirrors the api endpoints in Features/Lads: list +
// add are project-scoped, update addresses the claim. LADs surface on the project Programme tab's
// Claims view alongside the NOD/EOT requests.
public static class LadsRouteRegistration
{
    public static void RegisterLadsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListLadClaimsForProject, IReadOnlyList<LadClaim>>(
            new QueryRoute("/api/projects/{projectId}/lad-claims",
                query => $"/api/projects/{((ListLadClaimsForProject)query).ProjectId}/lad-claims"));

        commands.Register<AddLadClaim, LadClaim>(
            new CommandRoute("POST", "/api/projects/{projectId}/lad-claims",
                command => $"/api/projects/{((AddLadClaim)command).ProjectId}/lad-claims"));

        commands.Register<UpdateLadClaim, LadClaim>(
            new CommandRoute("PUT", "/api/lad-claims/{ladClaimId}",
                command => $"/api/lad-claims/{((UpdateLadClaim)command).LadClaimId}"));
    }
}
