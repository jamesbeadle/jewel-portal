using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Labour;

public static class LabourFeatureRegistration
{
    public static IServiceCollection AddLabourFeature(this IServiceCollection services)
    {
        services.AddScoped<SiteAccessGate>();

        // Worker registry + project assignment.
        services.AddScoped<IQueryHandler<ListWorkers, IReadOnlyList<Worker>>, ListWorkersHandler>();
        services.AddScoped<IQueryHandler<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>>, ListWorkerAssignmentsForProjectHandler>();
        services.AddScoped<ICommandHandler<AddWorker, Worker>, AddWorkerHandler>();
        services.AddScoped<ICommandHandler<UpdateWorker, Worker>, UpdateWorkerHandler>();
        services.AddScoped<ICommandHandler<SetProjectWorkerAssignment, ProjectWorkerAssignment>, SetProjectWorkerAssignmentHandler>();

        // Site access (QR token) + register.
        services.AddScoped<IQueryHandler<GetSiteAccess, SiteAccess>, GetSiteAccessHandler>();
        services.AddScoped<ICommandHandler<RotateSiteAccessToken, SiteAccess>, RotateSiteAccessTokenHandler>();
        services.AddScoped<IQueryHandler<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>>, ListSiteAttendanceForProjectHandler>();

        // Site capture (anonymous, token-authenticated).
        services.AddScoped<IQueryHandler<GetSiteSignInSheet, SiteSignInSheet>, GetSiteSignInSheetHandler>();
        services.AddScoped<ICommandHandler<SiteSignIn, Acknowledgement>, SiteSignInHandler>();
        services.AddScoped<ICommandHandler<SiteSignOut, Acknowledgement>, SiteSignOutHandler>();
        services.AddScoped<IQueryHandler<ListWorkerRejectedTimesheets, IReadOnlyList<WorkerTimesheetView>>, ListWorkerRejectedTimesheetsHandler>();
        services.AddScoped<ICommandHandler<ResubmitTimesheet, Acknowledgement>, ResubmitTimesheetHandler>();

        // Labour tab: week grid, adjust / approve / reject.
        services.AddScoped<ListTimesheetDetailsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListTimesheetDetailsForProject, IReadOnlyList<TimesheetDetail>>>(
            provider => provider.GetRequiredService<ListTimesheetDetailsForProjectHandler>());
        services.AddScoped<ICommandHandler<AdjustTimesheet, TimesheetDetail>, AdjustTimesheetHandler>();
        services.AddScoped<ICommandHandler<AddWorkerTimesheet, TimesheetDetail>, AddWorkerTimesheetHandler>();
        services.AddScoped<ApproveTimesheetsHandler>();
        services.AddScoped<ICommandHandler<ApproveTimesheets, LabourApprovalResult>>(
            provider => provider.GetRequiredService<ApproveTimesheetsHandler>());
        services.AddScoped<ICommandHandler<RejectTimesheet, TimesheetDetail>, RejectTimesheetHandler>();

        // Settlement reconciliation.
        services.AddScoped<IQueryHandler<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>>, ListLabourSettlementForProjectHandler>();
        services.AddScoped<SetXeroLineTimesheetCoverHandler>();
        services.AddScoped<ICommandHandler<SetXeroLineTimesheetCover, Acknowledgement>>(
            provider => provider.GetRequiredService<SetXeroLineTimesheetCoverHandler>());
        services.AddScoped<AddLabourSettlementVarianceHandler>();
        services.AddScoped<ICommandHandler<AddLabourSettlementVariance, LabourSettlementVariance>>(
            provider => provider.GetRequiredService<AddLabourSettlementVarianceHandler>());

        return services;
    }
}
