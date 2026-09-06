using Azure.Communication.Email;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Api.Features.Sales.Imagine;
using Jewel.JPMS.Api.Features.Sales.Inbox;
using Jewel.JPMS.Api.Features.Sales.Queries;
using Jewel.JPMS.Api.Features.Sales.Research;
using Jewel.JPMS.Contracts.Sales;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Sales;

/// <summary>The Sales section (2026-09-06): strategies for finding leads, the lead register
/// they feed, and — from the same day — the post-identification journey: the imagine page and
/// its renders, the sales inbox, proposals. Replaced the May 2026 Leads/CRM prototype
/// (Features/Leads) wholesale. Every external dependency (queues, blobs, email, image
/// generation, Graph) has a null stand-in so the API always starts and refuses with a reason.</summary>
public static class SalesFeatureRegistration
{
    public static IServiceCollection AddSalesFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // The research and render queues' producer side — same account resolution as the
        // mailbox and Bluebeam queues, so the worker (which consumes) sees the same queues.
        var queueConnection = configuration["MailboxQueuesConnection"] ?? configuration["AzureWebJobsStorage"];
        if (string.IsNullOrWhiteSpace(queueConnection))
        {
            services.AddSingleton<IStrategyResearchQueue, NullStrategyResearchQueue>();
            services.AddSingleton<IImagineRenderQueue, NullImagineRenderQueue>();
        }
        else
        {
            services.AddSingleton<IStrategyResearchQueue>(_ => new StorageStrategyResearchQueue(queueConnection!));
            services.AddSingleton<IImagineRenderQueue>(_ => new StorageImagineRenderQueue(queueConnection!));
        }

        // The imagine blob store (photos + renders): its own setting, else the Functions storage
        // account like the drawings store.
        var imagineStorage = configuration["ImagineStorage:ConnectionString"] ?? configuration["AzureWebJobsStorage"];
        if (string.IsNullOrWhiteSpace(imagineStorage))
            services.AddSingleton<IImagineImageStore, NullImagineImageStore>();
        else
            services.AddSingleton<IImagineImageStore>(_ => new AzureBlobImagineImageStore(imagineStorage!));

        // Prospect + sales-mailbox emails through ACS — the same connection string as the invite
        // emails; its own client instance so this registration doesn't depend on that one.
        var notifierOptions = ImagineNotifierOptions.FromConfiguration(configuration);
        services.AddSingleton(notifierOptions);
        var acsConnection = configuration["CommunicationServicesConnectionString"];
        if (string.IsNullOrWhiteSpace(acsConnection))
            services.AddSingleton<IImagineNotifier, NullImagineNotifier>();
        else
            services.AddSingleton<IImagineNotifier>(sp =>
                new AcsImagineNotifier(new EmailClient(acsConnection!), notifierOptions, sp.GetRequiredService<ILogger<AcsImagineNotifier>>()));

        services.AddScoped<ImaginePublicService>();

        // The sales inbox: the mailbox-intake Graph classes instantiated a second time for the
        // sales address, on the same app registration. Real only when those credentials are
        // present on the API; the page explains itself otherwise.
        var salesMailbox = SalesMailboxOptions.FromConfiguration(configuration);
        services.AddSingleton(salesMailbox);
        var graphOptions = MailboxIntakeOptions.FromConfiguration(configuration);
        if (salesMailbox.Enabled && graphOptions.IsConfigured)
        {
            services.AddSingleton<ISalesMailbox>(sp =>
            {
                var options = MailboxIntakeOptions.FromConfiguration(configuration);
                options.Mailbox = salesMailbox.Address;
                var http = new HttpClient();
                var tokens = new GraphTokenProvider(options);
                var graph = new MailboxGraphClient(http, tokens, options, sp.GetRequiredService<ILogger<MailboxGraphClient>>());
                var reader = new GraphIntakeMessageReader(http, tokens, options, sp.GetRequiredService<ILogger<GraphIntakeMessageReader>>());
                return new GraphSalesMailbox(graph, reader, new InboundEmailBodyBuilder(reader), salesMailbox.Address);
            });
        }
        else
        {
            services.AddSingleton<ISalesMailbox>(_ => new NullSalesMailbox(salesMailbox.Address));
        }

        services.AddScoped<IQueryHandler<ListLeads, IReadOnlyList<Lead>>, ListLeadsHandler>();
        services.AddScoped<IQueryHandler<GetLead, LeadDetail?>, GetLeadHandler>();
        services.AddScoped<IQueryHandler<ListSalesStrategies, IReadOnlyList<SalesStrategyOverview>>, ListSalesStrategiesHandler>();
        services.AddScoped<IQueryHandler<GetSalesStrategy, SalesStrategyDetail?>, GetSalesStrategyHandler>();
        services.AddScoped<IQueryHandler<ListSalesInbox, SalesInboxPage>, ListSalesInboxHandler>();
        services.AddScoped<IQueryHandler<GetSalesInboxConversation, MailboxPage>, GetSalesInboxConversationHandler>();
        services.AddScoped<IQueryHandler<GetSalesInboxMessage, MailboxMessageDetail>, GetSalesInboxMessageHandler>();

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
        Register<IssueImagineLink, Lead, IssueImagineLinkHandler, IssueImagineLinkAuthorisation, IssueImagineLinkValidation>(services);
        Register<RetryImagineRound, ImagineRoundView, RetryImagineRoundHandler, RetryImagineRoundAuthorisation, RetryImagineRoundValidation>(services);
        Register<SaveSalesProposal, SalesProposal, SaveSalesProposalHandler, SaveSalesProposalAuthorisation, SaveSalesProposalValidation>(services);
        Register<SendSalesProposal, SalesProposal, SendSalesProposalHandler, SendSalesProposalAuthorisation, SendSalesProposalValidation>(services);
        Register<WithdrawSalesProposal, SalesProposal, WithdrawSalesProposalHandler, WithdrawSalesProposalAuthorisation, WithdrawSalesProposalValidation>(services);
        Register<ReplyToSalesEmail, SalesReplyOutcome, ReplyToSalesEmailHandler, ReplyToSalesEmailAuthorisation, ReplyToSalesEmailValidation>(services);
        Register<LogSalesEmailToLead, LeadActivity, LogSalesEmailToLeadHandler, LogSalesEmailToLeadAuthorisation, LogSalesEmailToLeadValidation>(services);
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
