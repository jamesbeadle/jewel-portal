using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Services;

// One store for the unified Variation Order — the single document that runs Quoting → Issued →
// Approved / Rejected. (Before the 2026-07-23 unification this fronted two records, a VOQ and a VO.)
public interface IVariationStore
{
    event Action? OnChange;

    Task<VariationOrder?> GetByIdAsync(string variationOrderId, CancellationToken cancellationToken = default);
    Task<VariationOrder?> GetByRequestAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariationOrder>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);

    Task<VariationOrder> CreateFromRfqAsync(string requestId, string? title = null, string? description = null, decimal? estimatedValue = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a standalone variation order (in Quoting) with no request behind it — the
    /// manual-entry route for historic / client-instructed variations. A supplied number fixes the
    /// VOQ number (and the V-ref minted at approval); null takes the project's next number. The
    /// narrative sections (commercial basis, programme impact, exclusions) print on the official
    /// document and can be captured here or edited later.</summary>
    Task<VariationOrder> CreateManualAsync(string projectId, string title, string? description, decimal? estimatedValue, int? number, string? commercialBasis = null, string? programmeImpact = null, string? exclusions = null, CancellationToken cancellationToken = default);

    /// <summary>Re-states the official document's narrative sections — commercial basis, programme
    /// impact and exclusions. Wording only, allowed at every stage; blank clears a section.</summary>
    Task<VariationOrder> UpdateNarrativesAsync(string variationOrderId, string? commercialBasis, string? programmeImpact, string? exclusions, CancellationToken cancellationToken = default);

    /// <summary>Records the agreed subcontractor and value on a quoting variation order — who the
    /// works will be instructed to if the variation is approved. (Bid packages were separated from
    /// the VO quoting process 2026-08-12; the tender itself runs on the bid package.)</summary>
    Task<VariationOrder> SelectTenderAsync(string variationOrderId, string subcontractorId, decimal? estimatedValue, CancellationToken cancellationToken = default);

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

    /// <summary>Retitles a variation order — allowed at every stage. Only the title moves: figures
    /// already written to the valuation report and CVR keep the wording they were issued with.</summary>
    Task<VariationOrder> RenameAsync(string variationOrderId, string title, CancellationToken cancellationToken = default);

    /// <summary>Re-states a PRE-approval variation's estimate. Null or zero says the order is
    /// currently unpriced — the valuation export's Pending tab then leaves it out. Refused once a
    /// build-up is staged (the staged total is the estimate) and on approved/rejected orders.</summary>
    Task<VariationOrder> SetEstimateAsync(string variationOrderId, decimal? estimatedValue, CancellationToken cancellationToken = default);

    /// <summary>Deletes a non-approved variation order — a VOQ raised in error. Refused for an
    /// approved order (reject / return to quoting first). Bid packages are separate records and
    /// are never deleted with a variation.</summary>
    Task DeleteAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>Revises the value of an approved variation order; the delta writes through to the valuation report, CVR and budget.</summary>
    Task<VariationOrder> ReviseVariationOrderValueAsync(string variationOrderId, decimal value, CancellationToken cancellationToken = default);

    /// <summary>Re-states an approved variation's priced lines (add / edit / remove) without
    /// un-approving it; the report lines, CVR and per-centre budgets move by the difference.</summary>
    Task<VariationOrder> ReviseVariationOrderLinesAsync(string variationOrderId, IReadOnlyList<VariationLineInput> lines, CancellationToken cancellationToken = default);

    /// <summary>Stages the client-agreed build-up on a PRE-approval variation: the lines (their
    /// total becomes the estimate; an empty list clears the staging) and the VO document's
    /// narrative sections (null keeps, whitespace clears). Nothing reaches the Valuation Report.</summary>
    Task<VariationOrder> StageBuildUpAsync(
        string variationOrderId, IReadOnlyList<VariationLineInput> lines,
        string? commercialBasis, string? programmeImpact, string? exclusions,
        CancellationToken cancellationToken = default);

    /// <summary>The order's in-app conversation, oldest first — internal notes plus the shared
    /// thread the client portal reads. Email correspondence lives in the tagged mailbox instead.</summary>
    Task<IReadOnlyList<VariationOrderMessage>> ListMessagesAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>Adds a message to the order's conversation. The author is stamped server-side.</summary>
    Task<VariationOrderMessage> PostMessageAsync(PostVariationOrderMessage command, CancellationToken cancellationToken = default);
}
