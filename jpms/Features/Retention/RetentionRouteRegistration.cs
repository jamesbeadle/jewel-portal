using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Retention;

public static class RetentionRouteRegistration
{
    public static void RegisterRetentionRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        // "/retention" belongs to the closeout release record; the terms live one route over.
        queries.Register<GetProjectRetention, ProjectRetention?>(
            new QueryRoute("/api/projects/{projectId}/retention-terms",
                query => $"/api/projects/{((GetProjectRetention)query).ProjectId}/retention-terms"));

        commands.Register<SetProjectRetention, ProjectRetention>(
            new CommandRoute("POST", "/api/projects/{projectId}/retention-terms",
                command => $"/api/projects/{((SetProjectRetention)command).ProjectId}/retention-terms"));

        commands.Register<ConfirmRetentionRelease, ProjectRetention>(
            new CommandRoute("POST", "/api/projects/{projectId}/retention-terms/releases",
                command => $"/api/projects/{((ConfirmRetentionRelease)command).ProjectId}/retention-terms/releases"));
    }
}
