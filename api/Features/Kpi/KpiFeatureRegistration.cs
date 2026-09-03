using Jewel.JPMS.Api.Features.Kpi.Commands;
using Jewel.JPMS.Api.Features.Kpi.Queries;
using Jewel.JPMS.Contracts.Kpi;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Kpi;

public static class KpiFeatureRegistration
{
    public static IServiceCollection AddKpiFeature(this IServiceCollection services)
    {
        services.AddScoped<KpiPersonResolver>();

        services.AddScoped<IQueryHandler<ListKpiEmails, IReadOnlyList<KpiEmail>>, ListKpiEmailsHandler>();
        services.AddScoped<IQueryHandler<ListKpiPeople, IReadOnlyList<KpiPerson>>, ListKpiPeopleHandler>();

        // The Control Centre's Internal-pane "Mark as KPI" (and the connector's mark_email_as_kpi).
        services.AddScoped<ICommandHandler<MarkEmailAsKpi, KpiEmail>, MarkEmailAsKpiHandler>();
        services.AddScoped<MarkEmailAsKpiAuthorisation>();
        services.AddScoped<MarkEmailAsKpiValidation>();

        services.AddScoped<ICommandHandler<UpdateKpiEmail, KpiEmail>, UpdateKpiEmailHandler>();
        services.AddScoped<UpdateKpiEmailAuthorisation>();
        services.AddScoped<UpdateKpiEmailValidation>();

        services.AddScoped<ICommandHandler<RemoveKpiEmail, Acknowledgement>, RemoveKpiEmailHandler>();
        services.AddScoped<RemoveKpiEmailAuthorisation>();

        // Someone without a portal login, added by name.
        services.AddScoped<ICommandHandler<AddKpiPerson, KpiPerson>, AddKpiPersonHandler>();
        services.AddScoped<AddKpiPersonAuthorisation>();
        services.AddScoped<AddKpiPersonValidation>();

        return services;
    }
}
