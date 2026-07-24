using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Variations;

// Routes for the unified Variation Order. Quoting-stage endpoints keep their historic /voqs/…
// paths (the record's quoting identity); lifecycle endpoints sit under /variation-orders/…. Both
// address the same record by its id — the path is just a stable string the API matches.
public static class VariationsRouteRegistration
{
    public static void RegisterVariationsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetVoqByRequest, VariationOrder?>(
            new QueryRoute("/api/requests/{requestId}/voq",
                query => $"/api/requests/{((GetVoqByRequest)query).RequestId}/voq"));

        queries.Register<ListBidPackagesForVoq, IReadOnlyList<BidPackage>>(
            new QueryRoute("/api/voqs/{voqId}/bid-packages",
                query => $"/api/voqs/{((ListBidPackagesForVoq)query).VariationOrderId}/bid-packages"));

        commands.Register<PrepareVoqDraft, VoqDraftProposal>(
            new CommandRoute("POST", "/api/requests/{requestId}/voq/draft",
                command => $"/api/requests/{((PrepareVoqDraft)command).RequestId}/voq/draft"));

        commands.Register<CreateVoqFromRfq, VariationOrder>(
            new CommandRoute("POST", "/api/requests/{requestId}/voq",
                command => $"/api/requests/{((CreateVoqFromRfq)command).RequestId}/voq"));

        // Standalone manual variation — no request behind it (POST at the project).
        commands.Register<CreateManualVariationOrder, VariationOrder>(
            new CommandRoute("POST", "/api/projects/{projectId}/manual-variation",
                command => $"/api/projects/{((CreateManualVariationOrder)command).ProjectId}/manual-variation"));

        commands.Register<AddBidPackageToVoq, BidPackage>(
            new CommandRoute("POST", "/api/voqs/{voqId}/bid-packages",
                command => $"/api/voqs/{((AddBidPackageToVoq)command).VariationOrderId}/bid-packages"));

        commands.Register<SelectVoqTender, VariationOrder>(
            new CommandRoute("POST", "/api/voqs/{voqId}/select-tender",
                command => $"/api/voqs/{((SelectVoqTender)command).VariationOrderId}/select-tender"));

        // Repairs a variation order's link to its request (RFI) — for pre-link (seeded) records.
        commands.Register<LinkVoqToRequest, VariationOrder>(
            new CommandRoute("POST", "/api/voqs/{voqId}/link-request",
                command => $"/api/voqs/{((LinkVoqToRequest)command).VariationOrderId}/link-request"));

        // Variation Orders — the unified record, addressed by id.
        queries.Register<GetVariationOrderById, VariationOrder?>(
            new QueryRoute("/api/variation-orders/{voId}",
                query => $"/api/variation-orders/{((GetVariationOrderById)query).VariationOrderId}"));

        queries.Register<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>(
            new QueryRoute("/api/projects/{projectId}/variation-orders",
                query => $"/api/projects/{((ListVariationOrdersForProject)query).ProjectId}/variation-orders"));

        // Subcontractor variation requests (portal-raised; internal review queue).
        queries.Register<ListVariationRequestsForProject, IReadOnlyList<SubcontractorVariationRequest>>(
            new QueryRoute("/api/projects/{projectId}/variation-requests",
                query => $"/api/projects/{((ListVariationRequestsForProject)query).ProjectId}/variation-requests"));

        commands.Register<AcceptVariationRequest, VariationOrder>(
            new CommandRoute("POST", "/api/variation-requests/{variationRequestId}/accept",
                command => $"/api/variation-requests/{((AcceptVariationRequest)command).VariationRequestId}/accept"));

        commands.Register<RejectVariationRequest, SubcontractorVariationRequest>(
            new CommandRoute("POST", "/api/variation-requests/{variationRequestId}/reject",
                command => $"/api/variation-requests/{((RejectVariationRequest)command).VariationRequestId}/reject"));

        commands.Register<ApproveVariationOrder, VariationOrder>(
            new CommandRoute("POST", "/api/variation-orders/{voId}/approve",
                command => $"/api/variation-orders/{((ApproveVariationOrder)command).VariationOrderId}/approve"));

        // Un-approves a variation order back to Quoting — repairs records approved in error.
        commands.Register<ReturnVariationOrderToQuoting, VariationOrder>(
            new CommandRoute("POST", "/api/variation-orders/{voId}/return-to-quoting",
                command => $"/api/variation-orders/{((ReturnVariationOrderToQuoting)command).VariationOrderId}/return-to-quoting"));

        commands.Register<RejectVariationOrder, VariationOrder>(
            new CommandRoute("POST", "/api/variation-orders/{voId}/reject",
                command => $"/api/variation-orders/{((RejectVariationOrder)command).VariationOrderId}/reject"));

        commands.Register<ReviseVariationOrderValue, VariationOrder>(
            new CommandRoute("POST", "/api/variation-orders/{voId}/revise-value",
                command => $"/api/variation-orders/{((ReviseVariationOrderValue)command).VariationOrderId}/revise-value"));

        // Direct moves between the side-effect-free stages (Quoting, Issued) — the status pill.
        commands.Register<SetVariationOrderStatus, VariationOrder>(
            new CommandRoute("POST", "/api/variation-orders/{voId}/status",
                command => $"/api/variation-orders/{((SetVariationOrderStatus)command).VariationOrderId}/status"));
    }
}
