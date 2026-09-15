using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs.Audits;

// Client routes for site audits. Mirrors the api endpoints in Features/Hs/Audits: the list and
// the create are project-scoped; the form, items, issue and close address the audit.
public static class HsAuditRouteRegistration
{
    public static IServiceCollection AddHsAuditReadModels(this IServiceCollection services)
    {
        services.AddScoped<HsAuditReadModel>();
        return services;
    }

    public static void RegisterHsAuditRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListHsAuditsForProject, IReadOnlyList<HsAudit>>(
            new QueryRoute("/api/projects/{projectId}/hs-audits",
                query => $"/api/projects/{((ListHsAuditsForProject)query).ProjectId}/hs-audits"));

        queries.Register<GetHsAudit, HsAuditView>(
            new QueryRoute("/api/hs-audits/{auditId}",
                query => $"/api/hs-audits/{((GetHsAudit)query).HsAuditId}"));

        commands.Register<CreateHsAudit, HsAudit>(
            new CommandRoute("POST", "/api/projects/{projectId}/hs-audits",
                command => $"/api/projects/{((CreateHsAudit)command).ProjectId}/hs-audits"));

        commands.Register<UpdateHsAuditDetails, HsAudit>(
            new CommandRoute("PUT", "/api/hs-audits/{auditId}",
                command => $"/api/hs-audits/{((UpdateHsAuditDetails)command).HsAuditId}"));

        commands.Register<UpdateHsAuditItems, HsAuditView>(
            new CommandRoute("PUT", "/api/hs-audits/{auditId}/items",
                command => $"/api/hs-audits/{((UpdateHsAuditItems)command).HsAuditId}/items"));

        commands.Register<IssueHsAudit, HsAuditView>(
            new CommandRoute("POST", "/api/hs-audits/{auditId}/issue",
                command => $"/api/hs-audits/{((IssueHsAudit)command).HsAuditId}/issue"));

        commands.Register<CloseHsAudit, HsAudit>(
            new CommandRoute("POST", "/api/hs-audits/{auditId}/close",
                command => $"/api/hs-audits/{((CloseHsAudit)command).HsAuditId}/close"));
    }
}
