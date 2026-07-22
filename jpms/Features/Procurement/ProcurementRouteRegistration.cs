using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Procurement;

public static class ProcurementRouteRegistration
{
    public static IServiceCollection AddProcurementReadModels(this IServiceCollection services)
    {
        services.AddScoped<BidPackagesReadModel>();
        services.AddScoped<WorkOrdersReadModel>();
        services.AddScoped<ProjectWorkOrdersReadModel>();
        return services;
    }

    public static void RegisterProcurementRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListBidPackagesForProject, IReadOnlyList<BidPackage>>(
            new QueryRoute("/api/projects/{projectId}/bid-packages",
                query => $"/api/projects/{((ListBidPackagesForProject)query).ProjectId}/bid-packages"));

        queries.Register<GetBidPackageById, BidPackage?>(
            new QueryRoute("/api/bid-packages/{bidPackageId}",
                query => $"/api/bid-packages/{((GetBidPackageById)query).BidPackageId}"));

        queries.Register<ListQuotesForBidPackage, IReadOnlyList<Quote>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/quotes",
                query => $"/api/bid-packages/{((ListQuotesForBidPackage)query).BidPackageId}/quotes"));

        queries.Register<ListWorkOrders, IReadOnlyList<WorkOrder>>(QueryRoute.Static("/api/work-orders"));

        queries.Register<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>>(
            new QueryRoute("/api/projects/{projectId}/work-orders",
                query => $"/api/projects/{((ListProjectWorkOrders)query).ProjectId}/work-orders"));

        queries.Register<ListBidPackageRecipients, IReadOnlyList<BidPackageRecipient>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/recipients",
                query => $"/api/bid-packages/{((ListBidPackageRecipients)query).BidPackageId}/recipients"));

        queries.Register<ListBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/line-items",
                query => $"/api/bid-packages/{((ListBidPackageLineItems)query).BidPackageId}/line-items"));

        queries.Register<ListBidPackageEmails, IReadOnlyList<MailboxMessage>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/emails",
                query => $"/api/bid-packages/{((ListBidPackageEmails)query).BidPackageId}/emails"));

        queries.Register<ListQuoteLineItemsForBidPackage, IReadOnlyList<QuoteLineItem>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/quote-lines",
                query => $"/api/bid-packages/{((ListQuoteLineItemsForBidPackage)query).BidPackageId}/quote-lines"));

        queries.Register<SearchLocalSubcontractors, LocalSubcontractorSearchResult>(
            new QueryRoute("/api/projects/{projectId}/local-subcontractors",
                query =>
                {
                    var search = (SearchLocalSubcontractors)query;
                    var path = $"/api/projects/{search.ProjectId}/local-subcontractors?trade={Uri.EscapeDataString(search.Trade)}";
                    return string.IsNullOrEmpty(search.PageToken)
                        ? path
                        : $"{path}&pageToken={Uri.EscapeDataString(search.PageToken)}";
                }));

        queries.Register<ListBidPackageDrawings, IReadOnlyList<Drawing>>(
            new QueryRoute("/api/bid-packages/{bidPackageId}/drawings",
                query => $"/api/bid-packages/{((ListBidPackageDrawings)query).BidPackageId}/drawings"));

        commands.Register<CreateBidPackage, BidPackage>(
            new CommandRoute("POST", "/api/projects/{projectId}/bid-packages",
                command => $"/api/projects/{((CreateBidPackage)command).ProjectId}/bid-packages"));

        commands.Register<CreateBidPackageFromMessage, BidPackage>(
            new CommandRoute("POST", "/api/mailbox/message/create-bid-package",
                _ => "/api/mailbox/message/create-bid-package"));

        commands.Register<InviteSubcontractorsToBidPackage, IReadOnlyList<BidPackageRecipient>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/recipients",
                command => $"/api/bid-packages/{((InviteSubcontractorsToBidPackage)command).BidPackageId}/recipients"));

        commands.Register<RemoveBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>(
            new CommandRoute("DELETE", "/api/bid-packages/{bidPackageId}/recipients/{recipientId}",
                command =>
                {
                    var c = (RemoveBidPackageRecipient)command;
                    return $"/api/bid-packages/{c.BidPackageId}/recipients/{c.RecipientId}";
                }));

        commands.Register<DeclineBidPackageRecipient, IReadOnlyList<BidPackageRecipient>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/recipients/{recipientId}/decline",
                command =>
                {
                    var c = (DeclineBidPackageRecipient)command;
                    return $"/api/bid-packages/{c.BidPackageId}/recipients/{c.RecipientId}/decline";
                }));

        commands.Register<SetBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>(
            new CommandRoute("PUT", "/api/bid-packages/{bidPackageId}/line-items",
                command => $"/api/bid-packages/{((SetBidPackageLineItems)command).BidPackageId}/line-items"));

        commands.Register<AddBidPackageLineItems, IReadOnlyList<BidPackageLineItem>>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/line-items",
                command => $"/api/bid-packages/{((AddBidPackageLineItems)command).BidPackageId}/line-items"));

        commands.Register<SetBidPackageLineItemCoverage, IReadOnlyList<BidPackageLineItem>>(
            new CommandRoute("PUT", "/api/bid-package-line-items/{lineItemId}/coverage",
                command => $"/api/bid-package-line-items/{((SetBidPackageLineItemCoverage)command).LineItemId}/coverage"));

        commands.Register<UpdateBidPackageScope, BidPackage>(
            new CommandRoute("PUT", "/api/bid-packages/{bidPackageId}",
                command => $"/api/bid-packages/{((UpdateBidPackageScope)command).BidPackageId}"));

        commands.Register<SetBidPackageDrawings, IReadOnlyList<Drawing>>(
            new CommandRoute("PUT", "/api/bid-packages/{bidPackageId}/drawings",
                command => $"/api/bid-packages/{((SetBidPackageDrawings)command).BidPackageId}/drawings"));

        commands.Register<PrepareBidPackageInviteDraft, BidPackageInviteDraft>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/draft-invite",
                command => $"/api/bid-packages/{((PrepareBidPackageInviteDraft)command).BidPackageId}/draft-invite"));

        commands.Register<ExtractQuoteFromMessage, QuoteExtractionProposal>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/extract-quote",
                command => $"/api/bid-packages/{((ExtractQuoteFromMessage)command).BidPackageId}/extract-quote"));

        commands.Register<SaveExtractedQuote, Quote>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/extracted-quotes",
                command => $"/api/bid-packages/{((SaveExtractedQuote)command).BidPackageId}/extracted-quotes"));

        commands.Register<SubmitQuoteForBidPackage, Quote>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/quotes",
                command => $"/api/bid-packages/{((SubmitQuoteForBidPackage)command).BidPackageId}/quotes"));

        commands.Register<ReviseQuote, Quote>(
            new CommandRoute("PUT", "/api/quotes/{quoteId}",
                command => $"/api/quotes/{((ReviseQuote)command).QuoteId}"));

        commands.Register<AwardBidPackage, WorkOrder>(
            new CommandRoute("POST", "/api/bid-packages/{bidPackageId}/award",
                command => $"/api/bid-packages/{((AwardBidPackage)command).BidPackageId}/award"));

        commands.Register<UpdateWorkOrder, WorkOrder>(
            new CommandRoute("PUT", "/api/work-orders/{workOrderId}",
                command => $"/api/work-orders/{((UpdateWorkOrder)command).WorkOrderId}"));

        // Raises a work order directly — no bid package — with per-centre priced lines.
        commands.Register<CreateManualWorkOrder, WorkOrder>(
            new CommandRoute("POST", "/api/projects/{projectId}/work-orders",
                command => $"/api/projects/{((CreateManualWorkOrder)command).ProjectId}/work-orders"));

        // Edits a manually raised order wholesale — supplier, title, scope and priced lines.
        commands.Register<UpdateManualWorkOrder, WorkOrder>(
            new CommandRoute("PUT", "/api/projects/{projectId}/work-orders/{workOrderId}",
                command =>
                {
                    var c = (UpdateManualWorkOrder)command;
                    return $"/api/projects/{c.ProjectId}/work-orders/{c.WorkOrderId}";
                }));

        // Re-codes / splits one priced line across cost centres, by £ amount.
        commands.Register<RecodeWorkOrderLine, IReadOnlyList<WorkOrderLine>>(
            new CommandRoute("POST", "/api/projects/{projectId}/work-order-lines/{lineId}/recode",
                command =>
                {
                    var c = (RecodeWorkOrderLine)command;
                    return $"/api/projects/{c.ProjectId}/work-order-lines/{c.WorkOrderLineId}/recode";
                }));
        // Issues the new work order that instructs an approved variation order.
        commands.Register<IssueWorkOrderForVariationOrder, WorkOrder>(
            new CommandRoute("POST", "/api/variation-orders/{variationOrderId}/work-order",
                command => $"/api/variation-orders/{((IssueWorkOrderForVariationOrder)command).VariationOrderId}/work-order"));

    }
}
