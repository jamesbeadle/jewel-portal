using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Features.Sales;

public static class SalesRouteRegistration
{
    public static IServiceCollection AddSalesReadModels(this IServiceCollection services)
    {
        services.AddScoped<LeadListReadModel>();
        services.AddScoped<LeadDetailReadModel>();
        services.AddScoped<SalesStrategyListReadModel>();
        services.AddScoped<SalesStrategyDetailReadModel>();
        return services;
    }

    public static void RegisterSalesRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListLeads, IReadOnlyList<Lead>>(
            new QueryRoute("/api/sales/leads", _ => "/api/sales/leads"));
        queries.Register<GetLead, LeadDetail?>(
            new QueryRoute("/api/sales/leads/{leadId}",
                query => $"/api/sales/leads/{((GetLead)query).LeadId}"));
        queries.Register<ListSalesStrategies, IReadOnlyList<SalesStrategyOverview>>(
            new QueryRoute("/api/sales/strategies", _ => "/api/sales/strategies"));
        queries.Register<GetSalesStrategy, SalesStrategyDetail?>(
            new QueryRoute("/api/sales/strategies/{strategyId}",
                query => $"/api/sales/strategies/{((GetSalesStrategy)query).StrategyId}"));

        commands.Register<CaptureLead, Lead>(
            new CommandRoute("POST", "/api/sales/leads", _ => "/api/sales/leads"));
        commands.Register<UpdateLead, Lead>(
            new CommandRoute("PUT", "/api/sales/leads/{leadId}",
                command => $"/api/sales/leads/{((UpdateLead)command).LeadId}"));
        commands.Register<MoveLeadStage, Lead>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/stage",
                command => $"/api/sales/leads/{((MoveLeadStage)command).LeadId}/stage"));
        commands.Register<WinLead, LeadWonOutcome>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/win",
                command => $"/api/sales/leads/{((WinLead)command).LeadId}/win"));
        commands.Register<LogLeadActivity, LeadActivity>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/activities",
                command => $"/api/sales/leads/{((LogLeadActivity)command).LeadId}/activities"));

        commands.Register<CreateSalesStrategy, SalesStrategy>(
            new CommandRoute("POST", "/api/sales/strategies", _ => "/api/sales/strategies"));
        commands.Register<UpdateSalesStrategy, SalesStrategy>(
            new CommandRoute("PUT", "/api/sales/strategies/{strategyId}",
                command => $"/api/sales/strategies/{((UpdateSalesStrategy)command).StrategyId}"));
        commands.Register<SetSalesStrategyStatus, SalesStrategy>(
            new CommandRoute("POST", "/api/sales/strategies/{strategyId}/status",
                command => $"/api/sales/strategies/{((SetSalesStrategyStatus)command).StrategyId}/status"));
        commands.Register<RunStrategyResearch, SalesStrategy>(
            new CommandRoute("POST", "/api/sales/strategies/{strategyId}/research",
                command => $"/api/sales/strategies/{((RunStrategyResearch)command).StrategyId}/research"));
        commands.Register<GenerateStrategyApproachPlan, SalesStrategy>(
            new CommandRoute("POST", "/api/sales/strategies/{strategyId}/plan",
                command => $"/api/sales/strategies/{((GenerateStrategyApproachPlan)command).StrategyId}/plan"));
    }
}
