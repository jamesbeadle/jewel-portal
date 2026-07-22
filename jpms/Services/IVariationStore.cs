using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IVariationStore
{
    event Action? OnChange;

    Task<VariationOrderQuote?> GetByIdAsync(string voqId, CancellationToken cancellationToken = default);
    Task<VariationOrderQuote?> GetByRequestAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariationOrderQuote>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariationOrder>> ListVariationOrdersForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BidPackage>> ListBidPackagesAsync(string voqId, CancellationToken cancellationToken = default);

    /// <summary>AI-drafts a VOQ from the request and its tagged emails; nothing is saved.</summary>
    Task<VoqDraftProposal> PrepareVoqDraftAsync(string requestId, CancellationToken cancellationToken = default);
    Task<VariationOrderQuote> CreateFromRfqAsync(string requestId, string? title = null, string? description = null, decimal? estimatedValue = null, CancellationToken cancellationToken = default);
    Task<BidPackage> AddBidPackageAsync(string voqId, string title, string trade, CancellationToken cancellationToken = default);
    Task<VariationOrderQuote> SelectTenderAsync(string voqId, string bidPackageId, string subcontractorId, decimal? estimatedValue, CancellationToken cancellationToken = default);

    /// <summary>Attaches a VOQ to the request (RFI) it was raised from — repairs pre-link (seeded) records.</summary>
    Task<VariationOrderQuote> LinkToRequestAsync(string voqId, string requestId, CancellationToken cancellationToken = default);

    // Subcontractor variation requests (portal-raised). Accepting creates a Selected VOQ carrying
    // the sub's price; the normal approve pipeline then applies. Issuing creates the NEW work order
    // that instructs an approved VO.
    Task<IReadOnlyList<SubcontractorVariationRequest>> ListVariationRequestsForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<VariationOrderQuote> AcceptVariationRequestAsync(string variationRequestId, CancellationToken cancellationToken = default);
    Task<SubcontractorVariationRequest> RejectVariationRequestAsync(string variationRequestId, string reason, CancellationToken cancellationToken = default);
    Task<WorkOrder> IssueWorkOrderForVariationOrderAsync(string variationOrderId, CancellationToken cancellationToken = default);

    Task<VariationOrder?> GetVariationOrderByVoqAsync(string voqId, CancellationToken cancellationToken = default);
    Task<VariationOrder> ApproveVoqAsync(string voqId, string costCode, decimal? value, CancellationToken cancellationToken = default);

    /// <summary>Un-approves a VOQ back to Tendering: deletes the live VO (freeing its V-ref) and
    /// reverses what the approval wrote — for records approved in error (chiefly seeded history).</summary>
    Task<VariationOrderQuote> ReturnToTenderingAsync(string voqId, CancellationToken cancellationToken = default);
    Task<VariationOrder> IssueVariationOrderAsync(string voId, CancellationToken cancellationToken = default);
    Task<VariationOrder> CancelVariationOrderAsync(string voId, CancellationToken cancellationToken = default);

    /// <summary>Revises the value of a live VO; the delta writes through to the valuation report, CVR and budget.</summary>
    Task<VariationOrder> ReviseVariationOrderValueAsync(string voId, decimal value, CancellationToken cancellationToken = default);
}
