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
        services.AddScoped<AddWorkerAuthorisation>();
        services.AddScoped<AddWorkerValidation>();
        services.AddScoped<ICommandHandler<UpdateWorker, Worker>, UpdateWorkerHandler>();
        services.AddScoped<UpdateWorkerAuthorisation>();
        services.AddScoped<UpdateWorkerValidation>();
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
        services.AddScoped<AdjustTimesheetAuthorisation>();
        services.AddScoped<AdjustTimesheetValidation>();
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
        // Connector coding/approval (code_worker_week / approve_worker_week / reject_worker_day
        // actions): by-name wrappers over the grid's own handlers above — same gates, same
        // hard-blocks, so the two surfaces cannot drift.
        services.AddScoped<ICommandHandler<CodeWorkerWeekByName, WorkerWeekCodingResult>, CodeWorkerWeekByNameHandler>();
        services.AddScoped<CodeWorkerWeekByNameAuthorisation>();
        services.AddScoped<CodeWorkerWeekByNameValidation>();
        services.AddScoped<ICommandHandler<ApproveWorkerWeekByName, WorkerWeekApprovalResult>, ApproveWorkerWeekByNameHandler>();
        services.AddScoped<ApproveWorkerWeekByNameAuthorisation>();
        services.AddScoped<ApproveWorkerWeekByNameValidation>();
        services.AddScoped<ICommandHandler<RejectWorkerDayByName, TimesheetDetail>, RejectWorkerDayByNameHandler>();
        services.AddScoped<RejectWorkerDayByNameAuthorisation>();
        services.AddScoped<RejectWorkerDayByNameValidation>();

        // Labour overview: forecast, placement grid, chase list (scope §4–§5).
        services.AddScoped<IQueryHandler<GetLabourOverview, LabourOverviewSnapshot>, GetLabourOverviewHandler>();
        services.AddScoped<ICommandHandler<SetWorkerContract, Acknowledgement>, SetWorkerContractHandler>();
        services.AddScoped<ICommandHandler<SetWorkerCisStatus, Acknowledgement>, SetWorkerCisStatusHandler>();
        services.AddScoped<RecordWorkerAbsenceHandler>();
        services.AddScoped<RecordWorkerAbsenceAuthorisation>();
        services.AddScoped<ICommandHandler<RecordWorkerAbsence, WorkerAbsence>>(
            provider => provider.GetRequiredService<RecordWorkerAbsenceHandler>());
        // Connector absence entry (record_worker_absence action): by-name wrapper over the handler above.
        services.AddScoped<ICommandHandler<RecordWorkerAbsenceByName, WorkerAbsence>, RecordWorkerAbsenceByNameHandler>();
        services.AddScoped<RecordWorkerAbsenceByNameAuthorisation>();
        services.AddScoped<RecordWorkerAbsenceByNameValidation>();
        services.AddScoped<ICommandHandler<RemoveWorkerAbsence, Acknowledgement>, RemoveWorkerAbsenceHandler>();
        services.AddScoped<SignOffLabourWeekHandler>();
        services.AddScoped<ICommandHandler<SignOffLabourWeek, LabourWeekSignOff>>(
            provider => provider.GetRequiredService<SignOffLabourWeekHandler>());
        services.AddScoped<ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement>, RemoveLabourWeekSignOffHandler>();
        // Connector sign-off (sign_off_labour_week / remove_labour_week_sign_off actions):
        // by-name wrappers over the handlers above — same signable rule, same gates.
        services.AddScoped<ICommandHandler<SignOffWorkerWeekByName, LabourWeekSignOff>, SignOffWorkerWeekByNameHandler>();
        services.AddScoped<SignOffWorkerWeekByNameAuthorisation>();
        services.AddScoped<SignOffWorkerWeekByNameValidation>();
        services.AddScoped<ICommandHandler<RemoveWorkerWeekSignOffByName, Acknowledgement>, RemoveWorkerWeekSignOffByNameHandler>();
        services.AddScoped<RemoveWorkerWeekSignOffByNameAuthorisation>();
        services.AddScoped<RemoveWorkerWeekSignOffByNameValidation>();

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
        // Connector coding run (run_xero_coding action): by-name wrapper over the runner above.
        services.AddScoped<ICommandHandler<RunXeroCodingByName, XeroCodingRunReport>, RunXeroCodingByNameHandler>();
        services.AddScoped<RunXeroCodingByNameAuthorisation>();
        services.AddScoped<RunXeroCodingByNameValidation>();
        // Gate classes for the settlement/Xero write cluster (2026-08-31): the endpoints keep
        // their inline checks; these exist so the connector's action gateway composes the same
        // RoleSet constants and argument rules (SettlementCommandGates.cs).
        services.AddScoped<SetSiteXeroMappingAuthorisation>();
        services.AddScoped<SetSiteXeroMappingValidation>();
        services.AddScoped<SetCostCodeXeroMappingAuthorisation>();
        services.AddScoped<SetCostCodeXeroMappingValidation>();

        // Settlement reconciliation.
        services.AddScoped<IQueryHandler<ListLabourSettlementForProject, IReadOnlyList<LabourSettlementRow>>, ListLabourSettlementForProjectHandler>();
        services.AddScoped<SetXeroLineTimesheetCoverHandler>();
        services.AddScoped<ICommandHandler<SetXeroLineTimesheetCover, Acknowledgement>>(
            provider => provider.GetRequiredService<SetXeroLineTimesheetCoverHandler>());
        services.AddScoped<AddLabourSettlementVarianceHandler>();
        services.AddScoped<ICommandHandler<AddLabourSettlementVariance, LabourSettlementVariance>>(
            provider => provider.GetRequiredService<AddLabourSettlementVarianceHandler>());
        services.AddScoped<SetXeroLineTimesheetCoverAuthorisation>();
        services.AddScoped<SetXeroLineTimesheetCoverValidation>();
        services.AddScoped<AddLabourSettlementVarianceAuthorisation>();
        services.AddScoped<AddLabourSettlementVarianceValidation>();

        return services;
    }
}
