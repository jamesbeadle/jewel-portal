using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.Commercial.Queries;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
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

        services.AddScoped<ICommandHandler<SetCostCentreCostCompletion, CostCentreCostProgress>, SetCostCentreCostCompletionHandler>();
        services.AddScoped<SetCostCentreCostCompletionAuthorisation>();
        services.AddScoped<SetCostCentreCostCompletionValidation>();

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

        // Valuation Report — the bill, its claims, and the claim lifecycle.
        services.AddScoped<IQueryHandler<ListValuationLinesForProject, IReadOnlyList<ValuationLineItem>>, ListValuationLinesForProjectHandler>();
        services.AddScoped<IQueryHandler<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>>, GetProjectFinancialSummaryHandler>();
        services.AddScoped<IQueryHandler<ListCostCentreActualCosts, IReadOnlyList<CostCentreActualCostLine>>, ListCostCentreActualCostsHandler>();
        services.AddScoped<IQueryHandler<ListValuationClaimsForProject, IReadOnlyList<ValuationClaim>>, ListValuationClaimsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListClaimLines, IReadOnlyList<ClaimLine>>, ListClaimLinesHandler>();

        services.AddScoped<ValuationReportAuthorisation>();

        services.AddScoped<ICommandHandler<AddValuationLineItem, ValuationLineItem>, AddValuationLineItemHandler>();
        services.AddScoped<AddValuationLineItemValidation>();

        services.AddScoped<ICommandHandler<UpdateValuationLineItem, ValuationLineItem>, UpdateValuationLineItemHandler>();
        services.AddScoped<UpdateValuationLineItemValidation>();

        services.AddScoped<ICommandHandler<RemoveValuationLineItem, Acknowledgement>, RemoveValuationLineItemHandler>();

        services.AddScoped<ICommandHandler<StartValuationClaim, ValuationClaim>, StartValuationClaimHandler>();
        services.AddScoped<StartValuationClaimValidation>();

        services.AddScoped<ICommandHandler<RecordClaimEntry, ClaimLine>, RecordClaimEntryHandler>();
        services.AddScoped<RecordClaimEntryValidation>();

        services.AddScoped<ICommandHandler<PreapproveValuationClaim, ValuationClaim>, PreapproveValuationClaimHandler>();
        services.AddScoped<ICommandHandler<ConfirmValuationClaim, ValuationClaim>, ConfirmValuationClaimHandler>();

        return services;
    }
}
