using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Commercial;

public static class CommercialRouteRegistration
{
    public static IServiceCollection AddCommercialReadModels(this IServiceCollection services)
    {
        services.AddScoped<ValuationsReadModel>();
        services.AddScoped<CostCodeBudgetsReadModel>();
        services.AddScoped<TimesheetsReadModel>();
        services.AddScoped<ValuationLinesReadModel>();
        services.AddScoped<ProjectFinancialSummaryReadModel>();
        services.AddScoped<CostCentreGroupsReadModel>();
        services.AddScoped<ValuationClaimsReadModel>();
        services.AddScoped<ClaimLinesReadModel>();
        return services;
    }

    public static void RegisterCommercialRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListValuationsForProject, IReadOnlyList<Valuation>>(
            new QueryRoute("/api/projects/{projectId}/valuations",
                query => $"/api/projects/{((ListValuationsForProject)query).ProjectId}/valuations"));

        queries.Register<ListClaimPeriodsForProject, IReadOnlyList<ClaimPeriod>>(
            new QueryRoute("/api/projects/{projectId}/claim-periods",
                query => $"/api/projects/{((ListClaimPeriodsForProject)query).ProjectId}/claim-periods"));

        commands.Register<AddClaimPeriod, ClaimPeriod>(
            new CommandRoute("POST", "/api/projects/{projectId}/claim-periods",
                command => $"/api/projects/{((AddClaimPeriod)command).ProjectId}/claim-periods"));

        commands.Register<SetCostCodeBudget, CostCodeBudget>(
            new CommandRoute("POST", "/api/projects/{projectId}/cost-code-budgets",
                command => $"/api/projects/{((SetCostCodeBudget)command).ProjectId}/cost-code-budgets"));

        commands.Register<SetCostCentreCostCompletion, CostCentreCostProgress>(
            new CommandRoute("POST", "/api/projects/{projectId}/cost-centre-cost-completion",
                command => $"/api/projects/{((SetCostCentreCostCompletion)command).ProjectId}/cost-centre-cost-completion"));

        commands.Register<SetCostCentreFinalisation, CostCentreCostProgress>(
            new CommandRoute("POST", "/api/projects/{projectId}/cost-centre-finalisation",
                command => $"/api/projects/{((SetCostCentreFinalisation)command).ProjectId}/cost-centre-finalisation"));

        commands.Register<LinkXeroLineToWorkOrder, Acknowledgement>(
            new CommandRoute("POST", "/api/projects/{projectId}/xero-line-work-order-links",
                command => $"/api/projects/{((LinkXeroLineToWorkOrder)command).ProjectId}/xero-line-work-order-links"));

        // Cost centre groups — named roll-ups on the Financials tab.
        queries.Register<ListCostCentreGroupsForProject, IReadOnlyList<CostCentreGroup>>(
            new QueryRoute("/api/projects/{projectId}/cost-centre-groups",
                query => $"/api/projects/{((ListCostCentreGroupsForProject)query).ProjectId}/cost-centre-groups"));

        commands.Register<CreateCostCentreGroup, CostCentreGroup>(
            new CommandRoute("POST", "/api/projects/{projectId}/cost-centre-groups",
                command => $"/api/projects/{((CreateCostCentreGroup)command).ProjectId}/cost-centre-groups"));

        commands.Register<RemoveCostCentreGroup, Acknowledgement>(
            new CommandRoute("DELETE", "/api/projects/{projectId}/cost-centre-groups/{groupId}",
                command => $"/api/projects/{((RemoveCostCentreGroup)command).ProjectId}/cost-centre-groups/{((RemoveCostCentreGroup)command).CostCentreGroupId}"));

        queries.Register<ListCostCodeBudgetsForProject, IReadOnlyList<CostCodeBudget>>(
            new QueryRoute("/api/projects/{projectId}/cost-code-budgets",
                query => $"/api/projects/{((ListCostCodeBudgetsForProject)query).ProjectId}/cost-code-budgets"));

        queries.Register<ListTimesheetsForProject, IReadOnlyList<Timesheet>>(
            new QueryRoute("/api/projects/{projectId}/timesheets",
                query => $"/api/projects/{((ListTimesheetsForProject)query).ProjectId}/timesheets"));

        commands.Register<DraftValuation, Valuation>(
            new CommandRoute("POST", "/api/projects/{projectId}/valuations",
                command => $"/api/projects/{((DraftValuation)command).ProjectId}/valuations"));

        commands.Register<IssueValuation, Valuation>(
            new CommandRoute("POST", "/api/valuations/{valuationId}/issue",
                command => $"/api/valuations/{((IssueValuation)command).ValuationId}/issue"));

        commands.Register<ReviseValuation, Valuation>(
            new CommandRoute("PUT", "/api/valuations/{valuationId}",
                command => $"/api/valuations/{((ReviseValuation)command).ValuationId}"));

        commands.Register<SubmitTimesheet, Timesheet>(CommandRoute.Post("/api/timesheets"));

        commands.Register<ApproveTimesheet, Timesheet>(
            new CommandRoute("POST", "/api/timesheets/{timesheetId}/approval",
                command => $"/api/timesheets/{((ApproveTimesheet)command).TimesheetId}/approval"));

        // Valuation Report — bill lines, claims, claim entries, and the claim lifecycle.
        queries.Register<ListValuationLinesForProject, IReadOnlyList<ValuationLineItem>>(
            new QueryRoute("/api/projects/{projectId}/valuation-lines",
                query => $"/api/projects/{((ListValuationLinesForProject)query).ProjectId}/valuation-lines"));

        queries.Register<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>>(
            new QueryRoute("/api/projects/{projectId}/financial-summary",
                query => $"/api/projects/{((GetProjectFinancialSummary)query).ProjectId}/financial-summary"));

        queries.Register<ListCostCentreActualCosts, IReadOnlyList<CostCentreActualCostLine>>(
            new QueryRoute("/api/projects/{projectId}/cost-centres/{costCode}/actual-costs",
                query => $"/api/projects/{((ListCostCentreActualCosts)query).ProjectId}/cost-centres/{Uri.EscapeDataString(((ListCostCentreActualCosts)query).CostCode)}/actual-costs"));

        // Work-order invoice allocation — per-order balances and the project-wide queue.
        queries.Register<ListWorkOrderInvoiceSummaries, IReadOnlyList<WorkOrderInvoiceSummary>>(
            new QueryRoute("/api/projects/{projectId}/work-order-invoice-summaries",
                query => $"/api/projects/{((ListWorkOrderInvoiceSummaries)query).ProjectId}/work-order-invoice-summaries"));

        queries.Register<ListProjectCostOfSalesLines, IReadOnlyList<ProjectCostOfSalesLine>>(
            new QueryRoute("/api/projects/{projectId}/cost-of-sales-lines",
                query => $"/api/projects/{((ListProjectCostOfSalesLines)query).ProjectId}/cost-of-sales-lines"));

        queries.Register<ListValuationClaimsForProject, IReadOnlyList<ValuationClaim>>(
            new QueryRoute("/api/projects/{projectId}/valuation-claims",
                query => $"/api/projects/{((ListValuationClaimsForProject)query).ProjectId}/valuation-claims"));

        queries.Register<ListClaimLines, IReadOnlyList<ClaimLine>>(
            new QueryRoute("/api/valuation-claims/{claimId}/entries",
                query => $"/api/valuation-claims/{((ListClaimLines)query).ValuationClaimId}/entries"));

        commands.Register<AddValuationLineItem, ValuationLineItem>(
            new CommandRoute("POST", "/api/projects/{projectId}/valuation-lines",
                command => $"/api/projects/{((AddValuationLineItem)command).ProjectId}/valuation-lines"));

        commands.Register<UpdateValuationLineItem, ValuationLineItem>(
            new CommandRoute("PUT", "/api/valuation-lines/{lineItemId}",
                command => $"/api/valuation-lines/{((UpdateValuationLineItem)command).ValuationLineItemId}"));

        commands.Register<RemoveValuationLineItem, Acknowledgement>(
            new CommandRoute("DELETE", "/api/valuation-lines/{lineItemId}",
                command => $"/api/valuation-lines/{((RemoveValuationLineItem)command).ValuationLineItemId}"));

        commands.Register<StartValuationClaim, ValuationClaim>(
            new CommandRoute("POST", "/api/projects/{projectId}/valuation-claims",
                command => $"/api/projects/{((StartValuationClaim)command).ProjectId}/valuation-claims"));

        commands.Register<RecordClaimEntry, ClaimLine>(
            new CommandRoute("POST", "/api/valuation-claims/{claimId}/entries",
                command => $"/api/valuation-claims/{((RecordClaimEntry)command).ValuationClaimId}/entries"));

        commands.Register<PreapproveValuationClaim, ValuationClaim>(
            new CommandRoute("POST", "/api/valuation-claims/{claimId}/preapproval",
                command => $"/api/valuation-claims/{((PreapproveValuationClaim)command).ValuationClaimId}/preapproval"));

        commands.Register<ConfirmValuationClaim, ValuationClaim>(
            new CommandRoute("POST", "/api/valuation-claims/{claimId}/confirmation",
                command => $"/api/valuation-claims/{((ConfirmValuationClaim)command).ValuationClaimId}/confirmation"));
    }
}
