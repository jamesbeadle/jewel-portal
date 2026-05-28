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

        queries.Register<ListContraChargesForProject, IReadOnlyList<ContraCharge>>(
            new QueryRoute("/api/projects/{projectId}/contra-charges",
                query => $"/api/projects/{((ListContraChargesForProject)query).ProjectId}/contra-charges"));

        commands.Register<RecordContraCharge, ContraCharge>(
            new CommandRoute("POST", "/api/projects/{projectId}/contra-charges",
                command => $"/api/projects/{((RecordContraCharge)command).ProjectId}/contra-charges"));

        queries.Register<ListSubcontractorRetentionsForProject, IReadOnlyList<SubcontractorRetention>>(
            new QueryRoute("/api/projects/{projectId}/subcontractor-retentions",
                query => $"/api/projects/{((ListSubcontractorRetentionsForProject)query).ProjectId}/subcontractor-retentions"));

        commands.Register<RecordSubcontractorRetention, SubcontractorRetention>(
            new CommandRoute("POST", "/api/projects/{projectId}/subcontractor-retentions",
                command => $"/api/projects/{((RecordSubcontractorRetention)command).ProjectId}/subcontractor-retentions"));
    }
}
