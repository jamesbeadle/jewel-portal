using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// One store for the unified Variation Order — the single document that runs Quoting → Issued →
// Approved / Rejected. (Before the 2026-07-23 unification this fronted two records, a VOQ and a VO.)
public interface IVariationStore
{
    event Action? OnChange;

    Task<VariationOrder?> GetByIdAsync(string variationOrderId, CancellationToken cancellationToken = default);
    Task<VariationOrder?> GetByRequestAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariationOrder>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BidPackage>> ListBidPackagesAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>AI-drafts a variation order from the request and its tagged emails; nothing is saved.</summary>
    Task<VoqDraftProposal> PrepareVoqDraftAsync(string requestId, CancellationToken cancellationToken = default);
    Task<VariationOrder> CreateFromRfqAsync(string requestId, string? title = null, string? description = null, decimal? estimatedValue = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a standalone variation order (in Quoting) with no request behind it — the
    /// manual-entry route for historic / client-instructed variations. A supplied number fixes the
    /// VOQ number (and the V-ref minted at approval); null takes the project's next number.</summary>
    Task<VariationOrder> CreateManualAsync(string projectId, string title, string? description, decimal? estimatedValue, int? number, CancellationToken cancellationToken = default);
    Task<BidPackage> AddBidPackageAsync(string variationOrderId, string title, string trade, CancellationToken cancellationToken = default);
    Task<VariationOrder> SelectTenderAsync(string variationOrderId, string bidPackageId, string subcontractorId, decimal? estimatedValue, CancellationToken cancellationToken = default);

    /// <summary>Attaches a variation order to the request (RFI) it was raised from — repairs pre-link (seeded) records.</summary>
    Task<VariationOrder> LinkToRequestAsync(string variationOrderId, string requestId, CancellationToken cancellationToken = default);

    // Subcontractor variation requests (portal-raised). Accepting creates a quoting variation order
    // carrying the sub's price; the normal lifecycle then applies. Issuing creates the NEW work order
    // that instructs an approved variation.
    Task<IReadOnlyList<SubcontractorVariationRequest>> ListVariationRequestsForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<VariationOrder> AcceptVariationRequestAsync(string variationRequestId, CancellationToken cancellationToken = default);
    Task<SubcontractorVariationRequest> RejectVariationRequestAsync(string variationRequestId, string reason, CancellationToken cancellationToken = default);
    Task<WorkOrder> IssueWorkOrderForVariationOrderAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>Approves a variation order — mints the V-ref and writes the value through to the
    /// valuation report, CVR and cost-centre budget. A priced build-up (lines) writes one report
    /// line per entry under its own cost centre; costCode is then the primary centre and value the
    /// sum. With no lines the single-value behaviour applies.</summary>
    Task<VariationOrder> ApproveAsync(string variationOrderId, string costCode, decimal? value, IReadOnlyList<VariationLineInput>? lines = null, CancellationToken cancellationToken = default);

    /// <summary>Moves a variation order between the side-effect-free stages (Quoting, Issued).
    /// Entering Issued stamps the client-issue date. Approve / reject keep their own flows — they
    /// carry the commercial writes.</summary>
    Task<VariationOrder> SetStatusAsync(string variationOrderId, VariationOrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>Rejects a variation order. From an approved order this reverses the approval's
    /// valuation / CVR / budget writes; before approval it is a plain status move.</summary>
    Task<VariationOrder> RejectAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>Un-approves a variation order back to Quoting, reversing what the approval wrote and
    /// freeing its V-ref — for records approved in error (chiefly seeded history).</summary>
    Task<VariationOrder> ReturnToQuotingAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a non-approved variation order and its bid-package tender data — a VOQ raised
    /// in error. Refused for an approved order (reject / return to quoting first).</summary>
    Task DeleteAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>Revises the value of an approved variation order; the delta writes through to the valuation report, CVR and budget.</summary>
    Task<VariationOrder> ReviseVariationOrderValueAsync(string variationOrderId, decimal value, CancellationToken cancellationToken = default);
}
