using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Features.Kpi;

public static class KpiRouteRegistration
{
    public static IServiceCollection AddKpiReadModels(this IServiceCollection services)
    {
        services.AddScoped<KpiReadModel>();
        services.AddScoped<KpiPeopleReadModel>();
        return services;
    }

    public static void RegisterKpiRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListKpiEmails, IReadOnlyList<KpiEmail>>(
            new QueryRoute("/api/kpi/emails",
                query => ((ListKpiEmails)query).PersonId is { Length: > 0 } person
                    ? $"/api/kpi/emails?person={Uri.EscapeDataString(person)}"
                    : "/api/kpi/emails"));

        queries.Register<ListKpiPeople, IReadOnlyList<KpiPerson>>(QueryRoute.Static("/api/kpi/people"));

        // The Control Centre's Internal-pane "Mark as KPI" (administrators only).
        commands.Register<MarkEmailAsKpi, KpiEmail>(
            new CommandRoute("POST", "/api/kpi/emails", _ => "/api/kpi/emails"));

        commands.Register<UpdateKpiEmail, KpiEmail>(
            new CommandRoute("PUT", "/api/kpi/emails/{kpiEmailId}",
                command => $"/api/kpi/emails/{((UpdateKpiEmail)command).KpiEmailId}"));

        commands.Register<RemoveKpiEmail, Acknowledgement>(
            new CommandRoute("DELETE", "/api/kpi/emails/{kpiEmailId}",
                command => $"/api/kpi/emails/{((RemoveKpiEmail)command).KpiEmailId}"));

        // Someone without a portal login, added by name.
        commands.Register<AddKpiPerson, KpiPerson>(
            new CommandRoute("POST", "/api/kpi/people", _ => "/api/kpi/people"));
    }
}
