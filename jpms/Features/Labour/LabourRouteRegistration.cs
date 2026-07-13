using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Labour;

public static class LabourRouteRegistration
{
    public static IServiceCollection AddLabourReadModels(this IServiceCollection services)
    {
        services.AddScoped<WorkersReadModel>();
        services.AddScoped<WorkerAssignmentsReadModel>();
        services.AddScoped<LabourTimesheetsReadModel>();
        services.AddScoped<SiteAttendanceReadModel>();
        services.AddScoped<SiteAccessReadModel>();
        services.AddScoped<LabourSettlementReadModel>();
        return services;
    }

    public static void RegisterLabourRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListWorkers, IReadOnlyList<Worker>>(
            QueryRoute.Static("/api/labour/workers"));

        queries.Register<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>>(
            new QueryRoute("/api/projects/{projectId}/labour/assignments",
                query => $"/api/projects/{((ListWorkerAssignmentsForProject)query).ProjectId}/labour/assignments"));

        queries.Register<ListTimesheetDetailsForProject, IReadOnlyList<TimesheetDetail>>(
            new QueryRoute("/api/projects/{projectId}/labour/timesheets",
                query => $"/api/projects/{((ListTimesheetDetailsForProject)query).ProjectId}/labour/timesheets"));

        queries.Register<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>>(
            new QueryRoute("/api/projects/{projectId}/labour/attendance",
                query => $"/api/projects/{((ListSiteAttendanceForProject)query).ProjectId}/labour/attendance"));

        queries.Register<GetSiteAccess, SiteAccess>(
            new QueryRoute("/api/projects/{projectId}/labour/site-access",
                query => $"/api/projects/{((GetSiteAccess)query).ProjectId}/labour/site-access"));

        queries.Register<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>>(
            new QueryRoute("/api/projects/{projectId}/labour/settlement",
                query => $"/api/projects/{((ListLabourSettlementForProject)query).ProjectId}/labour/settlement"));

        commands.Register<AddWorker, Worker>(CommandRoute.Post("/api/labour/workers"));

        commands.Register<UpdateWorker, Worker>(
            new CommandRoute("PUT", "/api/labour/workers/{workerId}",
                command => $"/api/labour/workers/{((UpdateWorker)command).WorkerId}"));

        commands.Register<SetProjectWorkerAssignment, ProjectWorkerAssignment>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/assignments",
                command => $"/api/projects/{((SetProjectWorkerAssignment)command).ProjectId}/labour/assignments"));

        commands.Register<RotateSiteAccessToken, SiteAccess>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/site-access/rotation",
                command => $"/api/projects/{((RotateSiteAccessToken)command).ProjectId}/labour/site-access/rotation"));

        commands.Register<AddWorkerTimesheet, TimesheetDetail>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/timesheets",
                command => $"/api/projects/{((AddWorkerTimesheet)command).ProjectId}/labour/timesheets"));

        commands.Register<AdjustTimesheet, TimesheetDetail>(
            new CommandRoute("PUT", "/api/labour/timesheets/{timesheetId}",
                command => $"/api/labour/timesheets/{((AdjustTimesheet)command).TimesheetId}"));

        commands.Register<ApproveTimesheets, LabourApprovalResult>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/approvals",
                command => $"/api/projects/{((ApproveTimesheets)command).ProjectId}/labour/approvals"));

        commands.Register<RejectTimesheet, TimesheetDetail>(
            new CommandRoute("POST", "/api/labour/timesheets/{timesheetId}/rejection",
                command => $"/api/labour/timesheets/{((RejectTimesheet)command).TimesheetId}/rejection"));

        commands.Register<SetXeroLineTimesheetCover, Acknowledgement>(
            CommandRoute.Post("/api/labour/timesheet-covers"));

        commands.Register<AddLabourSettlementVariance, LabourSettlementVariance>(
            new CommandRoute("POST", "/api/projects/{projectId}/labour/settlement-variances",
                command => $"/api/projects/{((AddLabourSettlementVariance)command).ProjectId}/labour/settlement-variances"));
    }
}
