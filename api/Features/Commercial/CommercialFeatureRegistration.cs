using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.Commercial.Queries;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Commercial;

public static class CommercialFeatureRegistration
{
    public static IServiceCollection AddCommercialFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListValuationsForProject, IReadOnlyList<Valuation>>, ListValuationsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListCostCodeBudgetsForProject, IReadOnlyList<CostCodeBudget>>, ListCostCodeBudgetsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListTimesheetsForProject, IReadOnlyList<Timesheet>>, ListTimesheetsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListClaimPeriodsForProject, IReadOnlyList<ClaimPeriod>>, ListClaimPeriodsForProjectHandler>();

        services.AddScoped<ICommandHandler<AddClaimPeriod, ClaimPeriod>, AddClaimPeriodHandler>();
        services.AddScoped<AddClaimPeriodAuthorisation>();
        services.AddScoped<AddClaimPeriodValidation>();

        services.AddScoped<ICommandHandler<SetCostCodeBudget, CostCodeBudget>, SetCostCodeBudgetHandler>();
        services.AddScoped<SetCostCodeBudgetAuthorisation>();
        services.AddScoped<SetCostCodeBudgetValidation>();

        services.AddScoped<ICommandHandler<DraftValuation, Valuation>, DraftValuationHandler>();
        services.AddScoped<DraftValuationAuthorisation>();
        services.AddScoped<DraftValuationValidation>();

        services.AddScoped<ICommandHandler<IssueValuation, Valuation>, IssueValuationHandler>();
        services.AddScoped<IssueValuationAuthorisation>();
        services.AddScoped<IssueValuationValidation>();

        services.AddScoped<ICommandHandler<ReviseValuation, Valuation>, ReviseValuationHandler>();
        services.AddScoped<ReviseValuationAuthorisation>();
        services.AddScoped<ReviseValuationValidation>();

        services.AddScoped<ICommandHandler<SubmitTimesheet, Timesheet>, SubmitTimesheetHandler>();
        services.AddScoped<SubmitTimesheetAuthorisation>();
        services.AddScoped<SubmitTimesheetValidation>();

        services.AddScoped<ICommandHandler<ApproveTimesheet, Timesheet>, ApproveTimesheetHandler>();
        services.AddScoped<ApproveTimesheetAuthorisation>();
        services.AddScoped<ApproveTimesheetValidation>();

        return services;
    }
}
