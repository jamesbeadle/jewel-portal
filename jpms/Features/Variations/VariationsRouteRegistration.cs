using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Variations;

public static class VariationsRouteRegistration
{
    public static void RegisterVariationsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetVoqById, VariationOrderQuote?>(
            new QueryRoute("/api/voqs/{voqId}",
                query => $"/api/voqs/{((GetVoqById)query).VariationOrderQuoteId}"));

        queries.Register<GetVoqByRequest, VariationOrderQuote?>(
            new QueryRoute("/api/requests/{requestId}/voq",
                query => $"/api/requests/{((GetVoqByRequest)query).RequestId}/voq"));

        queries.Register<ListVoqsForProject, IReadOnlyList<VariationOrderQuote>>(
            new QueryRoute("/api/projects/{projectId}/voqs",
                query => $"/api/projects/{((ListVoqsForProject)query).ProjectId}/voqs"));

        queries.Register<ListBidPackagesForVoq, IReadOnlyList<BidPackage>>(
            new QueryRoute("/api/voqs/{voqId}/bid-packages",
                query => $"/api/voqs/{((ListBidPackagesForVoq)query).VariationOrderQuoteId}/bid-packages"));

        commands.Register<PrepareVoqDraft, VoqDraftProposal>(
            new CommandRoute("POST", "/api/requests/{requestId}/voq/draft",
                command => $"/api/requests/{((PrepareVoqDraft)command).RequestId}/voq/draft"));

        commands.Register<CreateVoqFromRfq, VariationOrderQuote>(
            new CommandRoute("POST", "/api/requests/{requestId}/voq",
                command => $"/api/requests/{((CreateVoqFromRfq)command).RequestId}/voq"));

        commands.Register<AddBidPackageToVoq, BidPackage>(
            new CommandRoute("POST", "/api/voqs/{voqId}/bid-packages",
                command => $"/api/voqs/{((AddBidPackageToVoq)command).VariationOrderQuoteId}/bid-packages"));

        commands.Register<SelectVoqTender, VariationOrderQuote>(
            new CommandRoute("POST", "/api/voqs/{voqId}/select-tender",
                command => $"/api/voqs/{((SelectVoqTender)command).VariationOrderQuoteId}/select-tender"));

        // Variation Orders (Phase 3).
        queries.Register<GetVariationOrderById, VariationOrder?>(
            new QueryRoute("/api/variation-orders/{voId}",
                query => $"/api/variation-orders/{((GetVariationOrderById)query).VariationOrderId}"));

        queries.Register<GetVariationOrderByVoq, VariationOrder?>(
            new QueryRoute("/api/voqs/{voqId}/variation-order",
                query => $"/api/voqs/{((GetVariationOrderByVoq)query).VariationOrderQuoteId}/variation-order"));

        queries.Register<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>(
            new QueryRoute("/api/projects/{projectId}/variation-orders",
                query => $"/api/projects/{((ListVariationOrdersForProject)query).ProjectId}/variation-orders"));

        // Subcontractor variation requests (portal-raised; internal review queue).
        queries.Register<ListVariationRequestsForProject, IReadOnlyList<SubcontractorVariationRequest>>(
            new QueryRoute("/api/projects/{projectId}/variation-requests",
                query => $"/api/projects/{((ListVariationRequestsForProject)query).ProjectId}/variation-requests"));

        commands.Register<AcceptVariationRequest, VariationOrderQuote>(
            new CommandRoute("POST", "/api/variation-requests/{variationRequestId}/accept",
                command => $"/api/variation-requests/{((AcceptVariationRequest)command).VariationRequestId}/accept"));

        commands.Register<RejectVariationRequest, SubcontractorVariationRequest>(
            new CommandRoute("POST", "/api/variation-requests/{variationRequestId}/reject",
                command => $"/api/variation-requests/{((RejectVariationRequest)command).VariationRequestId}/reject"));

        commands.Register<ApproveVariationOrderQuote, VariationOrder>(
            new CommandRoute("POST", "/api/voqs/{voqId}/approve",
                command => $"/api/voqs/{((ApproveVariationOrderQuote)command).VariationOrderQuoteId}/approve"));

        commands.Register<IssueVariationOrder, VariationOrder>(
            new CommandRoute("POST", "/api/variation-orders/{voId}/issue",
                command => $"/api/variation-orders/{((IssueVariationOrder)command).VariationOrderId}/issue"));

        commands.Register<CancelVariationOrder, VariationOrder>(
            new CommandRoute("POST", "/api/variation-orders/{voId}/cancel",
                command => $"/api/variation-orders/{((CancelVariationOrder)command).VariationOrderId}/cancel"));
    }
}
