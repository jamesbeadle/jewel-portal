using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Site;

public static class SiteRouteRegistration
{
    public static IServiceCollection AddSiteReadModels(this IServiceCollection services)
    {
        services.AddScoped<SiteReportsReadModel>();
        services.AddScoped<ProgrammeReadModel>();
        return services;
    }

    public static void RegisterSiteRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListSiteReportsForProject, IReadOnlyList<SiteReport>>(
            new QueryRoute("/api/projects/{projectId}/site-reports",
                query => $"/api/projects/{((ListSiteReportsForProject)query).ProjectId}/site-reports"));

        queries.Register<GetProgrammeForProject, IReadOnlyList<ProgrammeTask>>(
            new QueryRoute("/api/projects/{projectId}/programme",
                query => $"/api/projects/{((GetProgrammeForProject)query).ProjectId}/programme"));

        commands.Register<AssembleSiteReport, SiteReport>(
            new CommandRoute("POST", "/api/projects/{projectId}/site-reports",
                command => $"/api/projects/{((AssembleSiteReport)command).ProjectId}/site-reports"));

        commands.Register<ApproveSiteReport, SiteReport>(
            new CommandRoute("POST", "/api/site-reports/{siteReportId}/approval",
                command => $"/api/site-reports/{((ApproveSiteReport)command).SiteReportId}/approval"));

        commands.Register<AddProgrammeTask, ProgrammeTask>(
            new CommandRoute("POST", "/api/projects/{projectId}/programme",
                command => $"/api/projects/{((AddProgrammeTask)command).ProjectId}/programme"));

        commands.Register<UpdateProgrammeTask, ProgrammeTask>(
            new CommandRoute("PUT", "/api/programme-tasks/{programmeTaskId}",
                command => $"/api/programme-tasks/{((UpdateProgrammeTask)command).ProgrammeTaskId}"));
    }
}
