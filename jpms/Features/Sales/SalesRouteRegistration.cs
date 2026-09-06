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
        services.AddScoped<SalesInboxReadModel>();
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

        // ---- Imagine + proposals (2026-09-06): the journey after a lead is identified ----
        commands.Register<IssueImagineLink, Lead>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/imagine/link",
                command => $"/api/sales/leads/{((IssueImagineLink)command).LeadId}/imagine/link"));
        commands.Register<RetryImagineRound, ImagineRoundView>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/imagine/rounds/{roundId}/retry",
                command => $"/api/sales/leads/{((RetryImagineRound)command).LeadId}/imagine/rounds/{((RetryImagineRound)command).RoundId}/retry"));
        commands.Register<SaveSalesProposal, SalesProposal>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/proposals",
                command => $"/api/sales/leads/{((SaveSalesProposal)command).LeadId}/proposals"));
        commands.Register<SendSalesProposal, SalesProposal>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/proposals/{proposalId}/send",
                command => $"/api/sales/leads/{((SendSalesProposal)command).LeadId}/proposals/{((SendSalesProposal)command).ProposalId}/send"));
        commands.Register<WithdrawSalesProposal, SalesProposal>(
            new CommandRoute("POST", "/api/sales/leads/{leadId}/proposals/{proposalId}/withdraw",
                command => $"/api/sales/leads/{((WithdrawSalesProposal)command).LeadId}/proposals/{((WithdrawSalesProposal)command).ProposalId}/withdraw"));

        // ---- Sales inbox (2026-09-06): sales@ read live; message ids in the query string ----
        queries.Register<ListSalesInbox, SalesInboxPage>(
            new QueryRoute("/api/sales/inbox",
                query =>
                {
                    var q = (ListSalesInbox)query;
                    return $"/api/sales/inbox?cursor={Uri.EscapeDataString(q.Cursor ?? string.Empty)}&take={q.Take}&newestFirst={(q.NewestFirst ? "true" : "false")}&search={Uri.EscapeDataString(q.Search ?? string.Empty)}";
                }));
        queries.Register<GetSalesInboxConversation, MailboxPage>(
            new QueryRoute("/api/sales/inbox/conversation",
                query => $"/api/sales/inbox/conversation?id={Uri.EscapeDataString(((GetSalesInboxConversation)query).ConversationId)}"));
        queries.Register<GetSalesInboxMessage, MailboxMessageDetail>(
            new QueryRoute("/api/sales/inbox/message",
                query => $"/api/sales/inbox/message?id={Uri.EscapeDataString(((GetSalesInboxMessage)query).MessageId)}"));
        commands.Register<ReplyToSalesEmail, SalesReplyOutcome>(
            new CommandRoute("POST", "/api/sales/inbox/reply", _ => "/api/sales/inbox/reply"));
        commands.Register<LogSalesEmailToLead, LeadActivity>(
            new CommandRoute("POST", "/api/sales/inbox/log", _ => "/api/sales/inbox/log"));
    }
}
