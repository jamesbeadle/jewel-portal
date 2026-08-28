using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.WeeklyCashflow;

// Client routes for the Weekly Cashflow plan. Mirrors the api endpoints in
// Features/WeeklyCashflow: one read for the whole plan, item commands addressing the item,
// placements as one upsert-or-clear post.
public static class WeeklyCashflowRouteRegistration
{
    public static IServiceCollection AddWeeklyCashflowReadModels(this IServiceCollection services)
    {
        services.AddScoped<WeeklyCashflowPlanReadModel>();
        return services;
    }

    public static void RegisterWeeklyCashflowRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetWeeklyCashflowPlan, WeeklyCashflowPlan>(
            QueryRoute.Static("/api/weekly-cashflow/plan"));

        commands.Register<CreateWeeklyCashflowItem, WeeklyCashflowItem>(
            CommandRoute.Post("/api/weekly-cashflow/items"));

        commands.Register<UpdateWeeklyCashflowItem, WeeklyCashflowItem>(
            new CommandRoute("PUT", "/api/weekly-cashflow/items/{weeklyCashflowItemId}",
                command => $"/api/weekly-cashflow/items/{((UpdateWeeklyCashflowItem)command).WeeklyCashflowItemId}"));

        commands.Register<ArchiveWeeklyCashflowItem, WeeklyCashflowItem>(
            new CommandRoute("POST", "/api/weekly-cashflow/items/{weeklyCashflowItemId}/archive",
                command => $"/api/weekly-cashflow/items/{((ArchiveWeeklyCashflowItem)command).WeeklyCashflowItemId}/archive"));

        commands.Register<PlaceWeeklyCashflowEntry, WeeklyCashflowPlacement?>(
            CommandRoute.Post("/api/weekly-cashflow/placements"));
    }
}
