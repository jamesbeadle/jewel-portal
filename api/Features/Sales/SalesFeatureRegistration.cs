using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Api.Features.Sales.Queries;
using Jewel.JPMS.Api.Features.Sales.Research;
using Jewel.JPMS.Contracts.Sales;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Sales;

/// <summary>The Sales section (2026-09-06): strategies for finding leads, and the lead register
/// they feed. Replaced the May 2026 Leads/CRM prototype (Features/Leads) wholesale.</summary>
public static class SalesFeatureRegistration
{
    public static IServiceCollection AddSalesFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // The research queue's producer side — same account resolution as the mailbox and
        // Bluebeam queues, so the worker (which consumes) sees the same queue.
        var queueConnection = configuration["MailboxQueuesConnection"] ?? configuration["AzureWebJobsStorage"];
        if (string.IsNullOrWhiteSpace(queueConnection))
            services.AddSingleton<IStrategyResearchQueue, NullStrategyResearchQueue>();
        else
            services.AddSingleton<IStrategyResearchQueue>(_ => new StorageStrategyResearchQueue(queueConnection!));

        services.AddScoped<IQueryHandler<ListLeads, IReadOnlyList<Lead>>, ListLeadsHandler>();
        services.AddScoped<IQueryHandler<GetLead, LeadDetail?>, GetLeadHandler>();
        services.AddScoped<IQueryHandler<ListSalesStrategies, IReadOnlyList<SalesStrategyOverview>>, ListSalesStrategiesHandler>();
        services.AddScoped<IQueryHandler<GetSalesStrategy, SalesStrategyDetail?>, GetSalesStrategyHandler>();

        Register<CaptureLead, Lead, CaptureLeadHandler, CaptureLeadAuthorisation, CaptureLeadValidation>(services);
        Register<UpdateLead, Lead, UpdateLeadHandler, UpdateLeadAuthorisation, UpdateLeadValidation>(services);
        Register<MoveLeadStage, Lead, MoveLeadStageHandler, MoveLeadStageAuthorisation, MoveLeadStageValidation>(services);
        Register<WinLead, LeadWonOutcome, WinLeadHandler, WinLeadAuthorisation, WinLeadValidation>(services);
        Register<LogLeadActivity, LeadActivity, LogLeadActivityHandler, LogLeadActivityAuthorisation, LogLeadActivityValidation>(services);
        Register<CreateSalesStrategy, SalesStrategy, CreateSalesStrategyHandler, CreateSalesStrategyAuthorisation, CreateSalesStrategyValidation>(services);
        Register<UpdateSalesStrategy, SalesStrategy, UpdateSalesStrategyHandler, UpdateSalesStrategyAuthorisation, UpdateSalesStrategyValidation>(services);
        Register<SetSalesStrategyStatus, SalesStrategy, SetSalesStrategyStatusHandler, SetSalesStrategyStatusAuthorisation, SetSalesStrategyStatusValidation>(services);
        Register<GenerateStrategyApproachPlan, SalesStrategy, GenerateStrategyApproachPlanHandler, GenerateStrategyApproachPlanAuthorisation, GenerateStrategyApproachPlanValidation>(services);
        Register<RunStrategyResearch, SalesStrategy, RunStrategyResearchHandler, RunStrategyResearchAuthorisation, RunStrategyResearchValidation>(services);
        return services;
    }

    private static void Register<TCommand, TResult, THandler, TAuthorisation, TValidation>(IServiceCollection services)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
        where TAuthorisation : class
        where TValidation : class
    {
        services.AddScoped<ICommandHandler<TCommand, TResult>, THandler>();
        services.AddScoped<TAuthorisation>();
        services.AddScoped<TValidation>();
    }
}
