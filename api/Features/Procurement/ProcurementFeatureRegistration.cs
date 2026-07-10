using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Api.Features.Procurement.Queries;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Procurement;

public static class ProcurementFeatureRegistration
{
    public static IServiceCollection AddProcurementFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListBidPackagesForProject, IReadOnlyList<BidPackage>>, ListBidPackagesForProjectHandler>();
        services.AddScoped<IQueryHandler<GetBidPackageById, BidPackage?>, GetBidPackageByIdHandler>();
        services.AddScoped<IQueryHandler<ListQuotesForBidPackage, IReadOnlyList<Quote>>, ListQuotesForBidPackageHandler>();
        services.AddScoped<IQueryHandler<ListWorkOrders, IReadOnlyList<WorkOrder>>, ListWorkOrdersHandler>();
        services.AddScoped<IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>>, ListProjectWorkOrdersHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageRecipients, IReadOnlyList<BidPackageRecipient>>, ListBidPackageRecipientsHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>, ListBidPackageLineItemsHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageEmails, IReadOnlyList<MailboxMessage>>, ListBidPackageEmailsHandler>();
        services.AddScoped<IQueryHandler<ListQuoteLineItemsForBidPackage, IReadOnlyList<QuoteLineItem>>, ListQuoteLineItemsForBidPackageHandler>();
        services.AddScoped<IQueryHandler<ListBidPackageDrawings, IReadOnlyList<Drawing>>, ListBidPackageDrawingsHandler>();
        services.AddScoped<IQueryHandler<SearchLocalSubcontractors, LocalSubcontractorSearchResult>, SearchLocalSubcontractorsHandler>();

        services.AddScoped<ICommandHandler<CreateBidPackage, BidPackage>, CreateBidPackageHandler>();
        services.AddScoped<CreateBidPackageAuthorisation>();
        services.AddScoped<CreateBidPackageValidation>();

        services.AddScoped<ICommandHandler<CreateBidPackageFromMessage, BidPackage>, CreateBidPackageFromMessageHandler>();
        services.AddScoped<CreateBidPackageFromMessageAuthorisation>();
        services.AddScoped<CreateBidPackageFromMessageValidation>();

        services.AddScoped<ICommandHandler<InviteSubcontractorsToBidPackage, IReadOnlyList<BidPackageRecipient>>, InviteSubcontractorsToBidPackageHandler>();
        services.AddScoped<InviteSubcontractorsToBidPackageAuthorisation>();
        services.AddScoped<InviteSubcontractorsToBidPackageValidation>();

        services.AddScoped<ICommandHandler<RemoveBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>, RemoveBidPackageRecipientHandler>();
        services.AddScoped<RemoveBidPackageRecipientAuthorisation>();
        services.AddScoped<RemoveBidPackageRecipientValidation>();

        services.AddScoped<ICommandHandler<SetBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>, SetBidPackageLineItemsHandler>();
        services.AddScoped<SetBidPackageLineItemsAuthorisation>();
        services.AddScoped<SetBidPackageLineItemsValidation>();

        services.AddScoped<ICommandHandler<AddBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>, AddBidPackageLineItemsHandler>();
        services.AddScoped<AddBidPackageLineItemsAuthorisation>();
        services.AddScoped<AddBidPackageLineItemsValidation>();

        services.AddScoped<ICommandHandler<SetBidPackageLineItemCoverage, IReadOnlyList<BidPackageLineItem>>, SetBidPackageLineItemCoverageHandler>();
        services.AddScoped<SetBidPackageLineItemCoverageAuthorisation>();
        services.AddScoped<SetBidPackageLineItemCoverageValidation>();

        services.AddScoped<ICommandHandler<UpdateBidPackageScope, BidPackage>, UpdateBidPackageScopeHandler>();
        services.AddScoped<UpdateBidPackageScopeAuthorisation>();
        services.AddScoped<UpdateBidPackageScopeValidation>();

        services.AddScoped<ICommandHandler<SetBidPackageDrawings, IReadOnlyList<Drawing>>, SetBidPackageDrawingsHandler>();
        services.AddScoped<SetBidPackageDrawingsAuthorisation>();
        services.AddScoped<SetBidPackageDrawingsValidation>();

        // NOTE: there is deliberately no send-invite command — invites are only ever created as
        // mailbox drafts (PrepareBidPackageInviteDraft) for a human to review and send from Outlook.
        services.AddScoped<ICommandHandler<PrepareBidPackageInviteDraft, BidPackageInviteDraft>, PrepareBidPackageInviteDraftHandler>();
        services.AddScoped<PrepareBidPackageInviteDraftAuthorisation>();
        services.AddScoped<PrepareBidPackageInviteDraftValidation>();

        services.AddScoped<ICommandHandler<ExtractQuoteFromMessage, QuoteExtractionProposal>, ExtractQuoteFromMessageHandler>();
        services.AddScoped<ExtractQuoteFromMessageAuthorisation>();
        services.AddScoped<ExtractQuoteFromMessageValidation>();

        services.AddScoped<ICommandHandler<SaveExtractedQuote, Quote>, SaveExtractedQuoteHandler>();
        services.AddScoped<SaveExtractedQuoteAuthorisation>();
        services.AddScoped<SaveExtractedQuoteValidation>();

        services.AddScoped<ICommandHandler<SubmitQuoteForBidPackage, Quote>, SubmitQuoteForBidPackageHandler>();
        services.AddScoped<SubmitQuoteForBidPackageAuthorisation>();
        services.AddScoped<SubmitQuoteForBidPackageValidation>();

        services.AddScoped<ICommandHandler<ReviseQuote, Quote>, ReviseQuoteHandler>();
        services.AddScoped<ReviseQuoteAuthorisation>();
        services.AddScoped<ReviseQuoteValidation>();

        services.AddScoped<ICommandHandler<AwardBidPackage, WorkOrder>, AwardBidPackageHandler>();
        services.AddScoped<AwardBidPackageAuthorisation>();
        services.AddScoped<AwardBidPackageValidation>();

        services.AddScoped<ICommandHandler<UpdateWorkOrder, WorkOrder>, UpdateWorkOrderHandler>();
        services.AddScoped<UpdateWorkOrderAuthorisation>();
        services.AddScoped<UpdateWorkOrderValidation>();

        return services;
    }
}
