using Jewel.JPMS.Contracts.Commercial;
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
        return services;
    }

    public static void RegisterCommercialRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListValuationsForProject, IReadOnlyList<Valuation>>(
            new QueryRoute("/api/projects/{projectId}/valuations",
                query => $"/api/projects/{((ListValuationsForProject)query).ProjectId}/valuations"));

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
    }
}
