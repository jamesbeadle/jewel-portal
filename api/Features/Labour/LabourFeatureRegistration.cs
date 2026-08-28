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
        // Worker registry + project assignment.
        services.AddScoped<IQueryHandler<ListWorkers, IReadOnlyList<Worker>>, ListWorkersHandler>();
        services.AddScoped<IQueryHandler<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>>, ListWorkerAssignmentsForProjectHandler>();
        services.AddScoped<ICommandHandler<AddWorker, Worker>, AddWorkerHandler>();
        services.AddScoped<ICommandHandler<UpdateWorker, Worker>, UpdateWorkerHandler>();
        services.AddScoped<ICommandHandler<DeleteWorker, Acknowledgement>, DeleteWorkerHandler>();
        services.AddScoped<ICommandHandler<SetProjectWorkerAssignment, ProjectWorkerAssignment>, SetProjectWorkerAssignmentHandler>();

        // Site register.
        services.AddScoped<IQueryHandler<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>>, ListSiteAttendanceForProjectHandler>();

        // My Day — the worker's own authenticated timesheet surface. Handlers are resolved
        // concretely because they need the signed-in email alongside the command.
        services.AddScoped<GetMyLabourDayHandler>();
        services.AddScoped<MySiteSignInHandler>();
        services.AddScoped<MySiteSignOutHandler>();
        services.AddScoped<MyResubmitTimesheetHandler>();

        // Labour tab: week grid, adjust / approve / reject.
        services.AddScoped<ListTimesheetDetailsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListTimesheetDetailsForProject, IReadOnlyList<TimesheetDetail>>>(
            provider => provider.GetRequiredService<ListTimesheetDetailsForProjectHandler>());
        services.AddScoped<ICommandHandler<AdjustTimesheet, TimesheetDetail>, AdjustTimesheetHandler>();
        services.AddScoped<ICommandHandler<AddWorkerTimesheet, TimesheetDetail>, AddWorkerTimesheetHandler>();
        services.AddScoped<SubmitWorkerWeekHandler>();
        services.AddScoped<ICommandHandler<SubmitWorkerWeek, WorkerWeekResult>>(
            provider => provider.GetRequiredService<SubmitWorkerWeekHandler>());
        // Connector week entry (submit_worker_week action): by-name wrapper over the handler above.
        services.AddScoped<ICommandHandler<SubmitWorkerWeekByName, WorkerWeekResult>, SubmitWorkerWeekByNameHandler>();
        services.AddScoped<SubmitWorkerWeekByNameAuthorisation>();
        services.AddScoped<SubmitWorkerWeekByNameValidation>();
        services.AddScoped<ApproveTimesheetsHandler>();
        services.AddScoped<ICommandHandler<ApproveTimesheets, LabourApprovalResult>>(
            provider => provider.GetRequiredService<ApproveTimesheetsHandler>());
        services.AddScoped<ICommandHandler<RejectTimesheet, TimesheetDetail>, RejectTimesheetHandler>();

        // Labour overview: forecast, placement grid, chase list (scope §4–§5).
        services.AddScoped<IQueryHandler<GetLabourOverview, LabourOverviewSnapshot>, GetLabourOverviewHandler>();
        services.AddScoped<ICommandHandler<SetWorkerContract, Acknowledgement>, SetWorkerContractHandler>();
        services.AddScoped<ICommandHandler<SetWorkerCisStatus, Acknowledgement>, SetWorkerCisStatusHandler>();
        services.AddScoped<RecordWorkerAbsenceHandler>();
        services.AddScoped<ICommandHandler<RecordWorkerAbsence, WorkerAbsence>>(
            provider => provider.GetRequiredService<RecordWorkerAbsenceHandler>());
        services.AddScoped<ICommandHandler<RemoveWorkerAbsence, Acknowledgement>, RemoveWorkerAbsenceHandler>();
        services.AddScoped<SignOffLabourWeekHandler>();
        services.AddScoped<ICommandHandler<SignOffLabourWeek, LabourWeekSignOff>>(
            provider => provider.GetRequiredService<SignOffLabourWeekHandler>());
        services.AddScoped<ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement>, RemoveLabourWeekSignOffHandler>();

        // Settlement schedules, Xero mappings and the §6a coding run.
        services.AddScoped<SettlementScheduleBuilder>();
        services.AddScoped<IQueryHandler<GetSettlementSchedules, SettlementScheduleSnapshot>, GetSettlementSchedulesHandler>();
        services.AddScoped<AddWorkerSettlementLineHandler>();
        services.AddScoped<ICommandHandler<AddWorkerSettlementLine, Acknowledgement>>(
            provider => provider.GetRequiredService<AddWorkerSettlementLineHandler>());
        services.AddScoped<ICommandHandler<RemoveWorkerSettlementLine, Acknowledgement>, RemoveWorkerSettlementLineHandler>();
        services.AddScoped<IQueryHandler<ListXeroMappings, XeroMappingsSnapshot>, ListXeroMappingsHandler>();
        services.AddScoped<ICommandHandler<SetSiteXeroMapping, Acknowledgement>, SetSiteXeroMappingHandler>();
        services.AddScoped<ICommandHandler<SetCostCodeXeroMapping, Acknowledgement>, SetCostCodeXeroMappingHandler>();
        services.AddScoped<RunXeroCodingHandler>();
        services.AddScoped<ICommandHandler<RunXeroCoding, IReadOnlyList<XeroCodingRunResult>>>(
            provider => provider.GetRequiredService<RunXeroCodingHandler>());

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
